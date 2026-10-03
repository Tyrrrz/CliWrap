using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;

namespace CliWrap.Utils;

/// <summary>
/// Unix pseudo-terminal implementation using openpty().
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal class UnixPseudoTerminal : PseudoTerminal
{
    private const int TerminalNameBufferSize = 4096;

    private readonly int _masterFd;
    private readonly int _slaveFd;
    private readonly string _slaveName;
    private readonly UnixFdStream _masterStream;
    private readonly Lock _closeLock = new();
    private bool _masterStreamDisposed;
    private bool _slaveFdClosed;
    private bool _disposed;

    /// <summary>
    /// Creates a new Unix pseudo-terminal with the specified dimensions.
    /// </summary>
    /// <param name="columns">Terminal width in columns.</param>
    /// <param name="rows">Terminal height in rows.</param>
    public UnixPseudoTerminal(int columns, int rows)
    {
        ValidateDimensions(columns, rows);

        var result = NativeMethods.Unix.OpenPty(
            out _masterFd,
            out _slaveFd,
            IntPtr.Zero,
            IntPtr.Zero,
            IntPtr.Zero
        );

        if (result != 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to create pseudo-terminal. Error code: {error}"
            );
        }

        var masterStreamCreated = false;
        try
        {
            SetCloseOnExec(_masterFd);
            SetCloseOnExec(_slaveFd);

            var terminalNameBuffer = new byte[TerminalNameBufferSize];
            result = NativeMethods.Unix.TtyName(
                _slaveFd,
                terminalNameBuffer,
                (nuint)terminalNameBuffer.Length
            );
            if (result != 0)
            {
                throw new InvalidOperationException(
                    $"Failed to resolve pseudo-terminal name. Error code: {result}"
                );
            }

            var terminalNameLength = Array.IndexOf(terminalNameBuffer, (byte)0);
            _slaveName = System.Text.Encoding.UTF8.GetString(
                terminalNameBuffer,
                0,
                terminalNameLength >= 0 ? terminalNameLength : terminalNameBuffer.Length
            );

            // Set initial terminal size
            SetSize(columns, rows);

            // Create stream wrapper for master fd
            // Master is bidirectional - write sends to child, read receives from child
            _masterStream = new UnixFdStream(_masterFd, canRead: true, canWrite: true);
            masterStreamCreated = true;
        }
        catch
        {
            // Clean up fds if initialization fails after openpty
            // Only close master fd if the stream hasn't taken ownership of it
            // (UnixFdStream will close the fd in its Dispose method)
            if (!masterStreamCreated)
                NativeMethods.Unix.Close(_masterFd);
            NativeMethods.Unix.Close(_slaveFd);
            throw;
        }
    }

    private static void SetCloseOnExec(int fileDescriptor)
    {
        var flags = NativeMethods.Unix.Fcntl(fileDescriptor, NativeMethods.Unix.F_GETFD, 0);
        if (flags < 0)
        {
            throw new InvalidOperationException(
                $"Failed to get descriptor flags. Error code: {Marshal.GetLastWin32Error()}"
            );
        }

        if (
            NativeMethods.Unix.Fcntl(
                fileDescriptor,
                NativeMethods.Unix.F_SETFD,
                flags | NativeMethods.Unix.FD_CLOEXEC
            ) < 0
        )
        {
            throw new InvalidOperationException(
                $"Failed to set close-on-exec. Error code: {Marshal.GetLastWin32Error()}"
            );
        }
    }

    /// <summary>
    /// Gets the master file descriptor.
    /// </summary>
    /// <remarks>
    /// This fd is used by the parent process for reading/writing to the PTY.
    /// It should be closed in the child process to prevent fd leaks.
    /// </remarks>
    public int MasterFd => _masterFd;

    /// <summary>
    /// Gets the slave file descriptor for the child process.
    /// </summary>
    /// <remarks>
    /// The child process should use this fd as its stdin, stdout, and stderr.
    /// </remarks>
    public int SlaveFd => _slaveFd;

    /// <summary>
    /// Gets the slave device path used by the child to acquire its controlling terminal.
    /// </summary>
    public string SlaveName => _slaveName;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Writing to this stream sends data to the child process's stdin.
    /// </para>
    /// <para>
    /// On Unix, this is the same stream as <see cref="OutputStream"/> because
    /// the master fd is bidirectional. Callers should be aware that concurrent
    /// read/write operations affect the same underlying file descriptor.
    /// </para>
    /// </remarks>
    public override Stream InputStream => _masterStream;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Reading from this stream receives data from the child process's stdout/stderr.
    /// </para>
    /// <para>
    /// On Unix, this is the same stream as <see cref="InputStream"/> because
    /// the master fd is bidirectional. Callers should be aware that concurrent
    /// read/write operations affect the same underlying file descriptor.
    /// </para>
    /// </remarks>
    public override Stream OutputStream => _masterStream;

    /// <inheritdoc />
    public override void SetSize(int columns, int rows)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        ValidateDimensions(columns, rows);

        var winSize = new NativeMethods.Unix.WinSize
        {
            Col = (ushort)columns,
            Row = (ushort)rows,
            XPixel = 0,
            YPixel = 0,
        };

        var result = NativeMethods.Unix.Ioctl(
            _masterFd,
            NativeMethods.Unix.TIOCSWINSZ,
            ref winSize
        );

        if (result != 0)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException(
                $"Failed to set terminal size. Error code: {error}"
            );
        }
    }

    /// <summary>
    /// Closes the slave file descriptor.
    /// </summary>
    /// <remarks>
    /// This should be called in the parent process after spawning,
    /// as the parent only uses the master fd.
    /// </remarks>
    public void CloseSlave()
    {
        // Use lock to prevent race condition where concurrent calls could
        // close the same file descriptor multiple times
        lock (_closeLock)
        {
            if (!_slaveFdClosed)
            {
                NativeMethods.Unix.Close(_slaveFd);
                _slaveFdClosed = true;
            }
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// On Unix, the last slave descriptor closing causes EOF (or EIO, mapped to EOF by
    /// <see cref="UnixFdStream" />) on the master side. The master must remain open until
    /// buffered output has been drained.
    /// </remarks>
    public override void CloseConsole()
    {
        // No-op. Dispose closes the master after the output pipeline has completed.
    }

    private static void ValidateDimensions(int columns, int rows)
    {
        if (columns < 1 || columns > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(columns),
                columns,
                $"Column count must be between 1 and {ushort.MaxValue}."
            );
        }

        if (rows < 1 || rows > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(rows),
                rows,
                $"Row count must be between 1 and {ushort.MaxValue}."
            );
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (!_disposed)
        {
            lock (_closeLock)
            {
                if (!_masterStreamDisposed)
                {
                    _masterStream.Dispose();
                    _masterStreamDisposed = true;
                }
                if (!_slaveFdClosed)
                {
                    NativeMethods.Unix.Close(_slaveFd);
                    _slaveFdClosed = true;
                }
            }
            _disposed = true;
        }
    }
}
