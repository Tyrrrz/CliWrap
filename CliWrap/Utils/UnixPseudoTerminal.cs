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
    private readonly int _masterFd;
    private readonly int _slaveFd;
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

        var masterStreamCreated = false;
        try
        {
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
    /// On Unix, the child process exiting causes EOF on the master side.
    /// However, if the master stream read is blocked after the child exits,
    /// we may need to close the stream to unblock it.
    /// </remarks>
    public override void CloseConsole()
    {
        // On Unix, when the child process exits and closes the slave side of the PTY,
        // the master side should receive EOF. However, to ensure blocked reads are
        // unblocked, we dispose the stream which closes the fd.
        lock (_closeLock)
        {
            if (!_masterStreamDisposed)
            {
                _masterStream.Dispose();
                _masterStreamDisposed = true;
            }
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
