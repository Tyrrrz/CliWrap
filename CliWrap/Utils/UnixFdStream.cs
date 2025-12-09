using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace CliWrap.Utils;

/// <summary>
/// Stream wrapper for Unix file descriptors.
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal class UnixFdStream : Stream
{
    private readonly int _fd;
    private readonly bool _canRead;
    private readonly bool _canWrite;
    private bool _disposed;

    /// <summary>
    /// Creates a new stream wrapper for a Unix file descriptor.
    /// </summary>
    /// <param name="fd">The file descriptor to wrap.</param>
    /// <param name="canRead">Whether the stream supports reading.</param>
    /// <param name="canWrite">Whether the stream supports writing.</param>
    public UnixFdStream(int fd, bool canRead, bool canWrite)
    {
        _fd = fd;
        _canRead = canRead;
        _canWrite = canWrite;
    }

    /// <inheritdoc />
    public override bool CanRead => _canRead && !_disposed;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => _canWrite && !_disposed;

    /// <inheritdoc />
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc />
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (!_canRead)
            throw new NotSupportedException("Stream does not support reading.");

        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (offset + count > buffer.Length)
            throw new ArgumentException(
                "The sum of offset and count is greater than the buffer length."
            );

        if (count == 0)
            return 0;

        var bytesRead = NativeMethods.Unix.Read(_fd, ref buffer[offset], (nuint)count);

        if (bytesRead < 0)
        {
            var error = Marshal.GetLastWin32Error();
            // Retry if interrupted by signal
            if (error == NativeMethods.Unix.EINTR)
                return Read(buffer, offset, count);

            throw new IOException(
                $"Failed to read from file descriptor {_fd}. Error code: {error}"
            );
        }

        return (int)bytesRead;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (_disposed)
            throw new ObjectDisposedException(GetType().FullName);

        if (!_canWrite)
            throw new NotSupportedException("Stream does not support writing.");

        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset));

        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (offset + count > buffer.Length)
            throw new ArgumentException(
                "The sum of offset and count is greater than the buffer length."
            );

        if (count == 0)
            return;

        var totalWritten = 0;
        while (totalWritten < count)
        {
            var bytesWritten = NativeMethods.Unix.Write(
                _fd,
                ref buffer[offset + totalWritten],
                (nuint)(count - totalWritten)
            );

            if (bytesWritten < 0)
            {
                var error = Marshal.GetLastWin32Error();
                // Retry if interrupted by signal
                if (error == NativeMethods.Unix.EINTR)
                    continue;

                throw new IOException(
                    $"Failed to write to file descriptor {_fd}. Error code: {error}"
                );
            }

            if (bytesWritten == 0)
                break;

            totalWritten += (int)bytesWritten;
        }
    }

    /// <inheritdoc />
    public override void Flush()
    {
        // No buffering, nothing to flush
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            NativeMethods.Unix.Close(_fd);
            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
