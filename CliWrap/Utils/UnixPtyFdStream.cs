using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using CliWrap.Native;

namespace CliWrap.Utils;

/// <summary>
/// A <see cref="Stream" /> wrapper around a Unix file descriptor, used for PTY master fd I/O.
/// The fd is bidirectional: writes deliver data to the child's stdin, reads receive the child's
/// stdout and stderr (merged by the terminal).
/// </summary>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
internal sealed class UnixPtyFdStream : Stream
{
    private readonly int _fd;
    private bool _disposed;

    public UnixPtyFdStream(int fd) => _fd = fd;

    public override bool CanRead => !_disposed;
    public override bool CanWrite => !_disposed;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        while (true)
        {
            var bytesRead = PtyNativeMethods.Unix.Read(_fd, ref buffer[offset], (nuint)count);

            if (bytesRead > 0)
                return (int)bytesRead;

            if (bytesRead == 0)
                return 0; // EOF

            var errno = Marshal.GetLastWin32Error();

            // Retry on EINTR (interrupted by signal)
            if (errno == PtyNativeMethods.Unix.EINTR)
                continue;

            // EIO is the normal EOF signal when the slave side closes (child exited)
            if (errno == PtyNativeMethods.Unix.EIO)
                return 0;

            throw new IOException($"PTY read failed. Error code: {errno}");
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var totalWritten = 0;
        while (totalWritten < count)
        {
            var written = PtyNativeMethods.Unix.Write(
                _fd,
                ref buffer[offset + totalWritten],
                (nuint)(count - totalWritten)
            );

            if (written < 0)
            {
                var errno = Marshal.GetLastWin32Error();
                if (errno == PtyNativeMethods.Unix.EINTR)
                    continue;

                throw new IOException($"PTY write failed. Error code: {errno}");
            }

            totalWritten += (int)written;
        }
    }

    // Run blocking fd I/O on the thread pool so the calling thread stays free.
    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) => Task.Run(() => Read(buffer, offset, count), cancellationToken);

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) => Task.Run(() => Write(buffer, offset, count), cancellationToken);

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        if (!MemoryMarshal.TryGetArray(buffer, out var segment))
            segment = new ArraySegment<byte>(buffer.ToArray());

        return new ValueTask<int>(
            ReadAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken)
        );
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment))
            segment = new ArraySegment<byte>(buffer.ToArray());

        return new ValueTask(
            WriteAsync(segment.Array!, segment.Offset, segment.Count, cancellationToken)
        );
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            PtyNativeMethods.Unix.Close(_fd);
            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
