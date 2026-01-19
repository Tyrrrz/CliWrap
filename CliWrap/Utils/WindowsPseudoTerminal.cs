using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace CliWrap.Utils;

/// <summary>
/// Windows pseudo-terminal implementation using ConPTY.
/// </summary>
[SupportedOSPlatform("windows")]
internal class WindowsPseudoTerminal : PseudoTerminal
{
    private readonly IntPtr _pseudoConsoleHandle;
    private readonly SafeFileHandle _inputWriteHandle;
    private readonly SafeFileHandle _outputReadHandle;
    private readonly FileStream _inputStream;
    private readonly FileStream _outputStream;
    private bool _disposed;

    /// <summary>
    /// Creates a new Windows pseudo-terminal with the specified dimensions.
    /// </summary>
    /// <param name="columns">Terminal width in columns.</param>
    /// <param name="rows">Terminal height in rows.</param>
    public WindowsPseudoTerminal(int columns, int rows)
    {
        ValidateDimensions(columns, rows);

        // Create pipe for PTY input (parent writes -> PTY reads)
        if (!CreatePipe(out var inputReadHandle, out _inputWriteHandle))
        {
            throw new InvalidOperationException(
                $"Failed to create input pipe. Error: {Marshal.GetLastWin32Error()}"
            );
        }

        // Create pipe for PTY output (PTY writes -> parent reads)
        if (!CreatePipe(out _outputReadHandle, out var outputWriteHandle))
        {
            inputReadHandle.Dispose();
            _inputWriteHandle.Dispose();
            throw new InvalidOperationException(
                $"Failed to create output pipe. Error: {Marshal.GetLastWin32Error()}"
            );
        }

        // Create the pseudo console
        // Note: ValidateDimensions already checked the dimensions, but we need to ensure they fit in short
        var size = new NativeMethods.Windows.Coord { X = (short)columns, Y = (short)rows };

        var result = NativeMethods.Windows.CreatePseudoConsole(
            size,
            inputReadHandle,
            outputWriteHandle,
            0,
            out _pseudoConsoleHandle
        );

        if (result != 0)
        {
            inputReadHandle.Dispose();
            _inputWriteHandle.Dispose();
            _outputReadHandle.Dispose();
            outputWriteHandle.Dispose();
            throw new InvalidOperationException(
                $"Failed to create pseudo console. HRESULT: 0x{result:X8}"
            );
        }

        // Close handles that are now owned by the pseudo console
        inputReadHandle.Dispose();
        outputWriteHandle.Dispose();

        // Create streams for I/O
        // Note: Using synchronous I/O because CreatePipe doesn't support overlapped I/O
        try
        {
            _inputStream = new FileStream(
                _inputWriteHandle,
                FileAccess.Write,
                bufferSize: 4096,
                isAsync: false
            );
            _outputStream = new FileStream(
                _outputReadHandle,
                FileAccess.Read,
                bufferSize: 4096,
                isAsync: false
            );
        }
        catch
        {
            _inputWriteHandle.Dispose();
            _outputReadHandle.Dispose();
            NativeMethods.Windows.ClosePseudoConsole(_pseudoConsoleHandle);
            throw;
        }
    }

    /// <summary>
    /// Gets the pseudo console handle for process creation.
    /// </summary>
    /// <remarks>
    /// This handle should be passed to the process via PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE.
    /// </remarks>
    public IntPtr Handle => _pseudoConsoleHandle;

    /// <inheritdoc />
    /// <remarks>
    /// Writing to this stream sends data to the child process's stdin.
    /// </remarks>
    public override Stream InputStream => _inputStream;

    /// <inheritdoc />
    /// <remarks>
    /// Reading from this stream receives data from the child process's stdout/stderr.
    /// </remarks>
    public override Stream OutputStream => _outputStream;

    /// <inheritdoc />
    public override void SetSize(int columns, int rows)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        ValidateDimensions(columns, rows);

        var size = new NativeMethods.Windows.Coord { X = (short)columns, Y = (short)rows };

        var result = NativeMethods.Windows.ResizePseudoConsole(_pseudoConsoleHandle, size);

        if (result != 0)
        {
            throw new InvalidOperationException(
                $"Failed to resize pseudo console. HRESULT: 0x{result:X8}"
            );
        }
    }

    private int _consoleClosed; // 0 = open, 1 = closed

    /// <inheritdoc />
    public override void CloseConsole()
    {
        // Use Interlocked.CompareExchange for thread-safe check-and-set
        // This prevents concurrent calls from invoking ClosePseudoConsole twice
        if (!_disposed && Interlocked.CompareExchange(ref _consoleClosed, 1, 0) == 0)
        {
            // Close the pseudo console to signal EOF on the output stream.
            // This causes blocked reads to return, enabling clean shutdown.
            // Note: This must be done BEFORE disposing the streams to avoid race conditions.
            NativeMethods.Windows.ClosePseudoConsole(_pseudoConsoleHandle);
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (!_disposed)
        {
            // Close the console first if not already closed (thread-safe)
            if (Interlocked.CompareExchange(ref _consoleClosed, 1, 0) == 0)
            {
                NativeMethods.Windows.ClosePseudoConsole(_pseudoConsoleHandle);
            }
            // Then dispose the streams
            _inputStream.Dispose();
            _outputStream.Dispose();
            _disposed = true;
        }
    }

    private static void ValidateDimensions(int columns, int rows)
    {
        if (columns < 1 || columns > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columns),
                columns,
                $"Column count must be between 1 and {short.MaxValue}."
            );
        }

        if (rows < 1 || rows > short.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rows),
                rows,
                $"Row count must be between 1 and {short.MaxValue}."
            );
        }
    }

    private static bool CreatePipe(out SafeFileHandle readHandle, out SafeFileHandle writeHandle)
    {
        // Create pipe with non-inheritable handles (matching Microsoft's EchoCon sample)
        // CreatePseudoConsole internally duplicates the handles, so inheritance is not needed
        return NativeMethods.Windows.CreatePipe(out readHandle, out writeHandle, IntPtr.Zero, 0);
    }
}
