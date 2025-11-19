using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils;

internal class SimplexStream : Stream
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly SemaphoreSlim _readLock = new(0, 1);

    private IMemoryOwner<byte> _sharedBuffer = MemoryPool<byte>.Shared.Rent(BufferSizes.Stream);
    private int _sharedBufferBytes;
    private int _sharedBufferBytesRead;

    [ExcludeFromCodeCoverage]
    public override bool CanRead => true;

    [ExcludeFromCodeCoverage]
    public override bool CanSeek => false;

    [ExcludeFromCodeCoverage]
    public override bool CanWrite => true;

    [ExcludeFromCodeCoverage]
    public override long Position { get; set; }

    [ExcludeFromCodeCoverage]
    public override long Length => throw new NotSupportedException();

    public override async Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        // Reset the buffer if the current one is too small for the incoming data
        if (_sharedBuffer.Memory.Length < count)
        {
            _sharedBuffer.Dispose();
            _sharedBuffer = MemoryPool<byte>.Shared.Rent(count);
        }

        buffer.AsSpan(offset, count).CopyTo(_sharedBuffer.Memory.Span);

        _sharedBufferBytes = count;
        _sharedBufferBytesRead = 0;

        _readLock.Release();
    }

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        await _readLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        var length = Math.Min(count, _sharedBufferBytes - _sharedBufferBytesRead);

        _sharedBuffer
            .Memory.Slice(_sharedBufferBytesRead, length)
            .CopyTo(buffer.AsMemory(offset, length));

        _sharedBufferBytesRead += length;

        // Release the write lock if the consumer has finished reading all of
        // the previously written data.
        if (_sharedBufferBytesRead >= _sharedBufferBytes)
        {
            _writeLock.Release();
        }
        // Otherwise, release the read lock again so that the consumer can finish
        // reading the data.
        else
        {
            _readLock.Release();
        }

        return length;
    }

    public async Task ReportCompletionAsync(CancellationToken cancellationToken = default) =>
        // Write an empty buffer that will make ReadAsync(...) return 0, which signifies the end of stream
        await WriteAsync([], 0, 0, cancellationToken).ConfigureAwait(false);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _readLock.Dispose();
            _writeLock.Dispose();
            _sharedBuffer.Dispose();
        }

        base.Dispose(disposing);
    }

    [ExcludeFromCodeCoverage]
    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer, offset, count).GetAwaiter().GetResult();

    [ExcludeFromCodeCoverage]
    public override void Write(byte[] buffer, int offset, int count) =>
        WriteAsync(buffer, offset, count).GetAwaiter().GetResult();

    [ExcludeFromCodeCoverage]
    public override void Flush() => throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override void SetLength(long value) => throw new NotSupportedException();
}
