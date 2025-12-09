using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CliWrap.Utils;

/// <summary>
/// Unix pseudo-terminal implementation using openpty().
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal class UnixPseudoTerminal : PseudoTerminal
{
    private readonly int _masterFd;
    private readonly int _slaveFd;
    private readonly UnixFdStream _masterStream;
    private bool _slaveFdClosed;
    private bool _disposed;

    /// <summary>
    /// Creates a new Unix pseudo-terminal with the specified dimensions.
    /// </summary>
    /// <param name="columns">Terminal width in columns.</param>
    /// <param name="rows">Terminal height in rows.</param>
    public UnixPseudoTerminal(int columns, int rows)
    {
        // Create the PTY master/slave pair
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

        try
        {
            // Set initial terminal size
            SetSize(columns, rows);

            // Create stream wrapper for master fd
            // Master is bidirectional - write sends to child, read receives from child
            _masterStream = new UnixFdStream(_masterFd, canRead: true, canWrite: true);
        }
        catch
        {
            // Clean up fds if initialization fails after openpty
            NativeMethods.Unix.Close(_masterFd);
            NativeMethods.Unix.Close(_slaveFd);
            throw;
        }
    }

    /// <summary>
    /// Gets the slave file descriptor for the child process.
    /// </summary>
    /// <remarks>
    /// The child process should use this fd as its stdin, stdout, and stderr.
    /// </remarks>
    public int SlaveFd => _slaveFd;

    /// <inheritdoc />
    /// <remarks>
    /// Writing to this stream sends data to the child process's stdin.
    /// </remarks>
    public override Stream InputStream => _masterStream;

    /// <inheritdoc />
    /// <remarks>
    /// Reading from this stream receives data from the child process's stdout/stderr.
    /// </remarks>
    public override Stream OutputStream => _masterStream;

    /// <inheritdoc />
    public override void SetSize(int columns, int rows)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

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
        if (!_slaveFdClosed)
        {
            NativeMethods.Unix.Close(_slaveFd);
            _slaveFdClosed = true;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// On Unix, closing the master stream will signal EOF to readers.
    /// This is typically not needed since the child process exit will cause EOF.
    /// </remarks>
    public override void CloseConsole()
    {
        // On Unix, the master fd closing signals EOF.
        // The stream will be cleaned up in Dispose.
        // For now, this is a no-op since Unix PTY typically signals EOF when the child exits.
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (!_disposed)
        {
            _masterStream.Dispose();
            if (!_slaveFdClosed)
            {
                NativeMethods.Unix.Close(_slaveFd);
                _slaveFdClosed = true;
            }
            _disposed = true;
        }
    }
}
