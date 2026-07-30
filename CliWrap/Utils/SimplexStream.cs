using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils;

internal partial class SimplexStream : Stream
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly SemaphoreSlim _readLock = new(0, 1);

    private ReadOnlyMemory<byte> _buffer;
    private int _bufferBytesRead;

    // While we do have Span/Memory polyfilled on all targets, Stream doesn't have intrinsic
    // Span/Memory-based overloads until .NET Standard 2.1 and .NET Core 2.1.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
    public override
#else
    private
#endif
    async ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        _buffer = buffer;
        _bufferBytesRead = 0;

        _readLock.Release();

        // Wait until the reader consumes the buffer, so it is safe for the caller
        // to reuse its memory once this method completes.
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        _writeLock.Release();
    }

    // While we do have Span/Memory polyfilled on all targets, Stream doesn't have intrinsic
    // Span/Memory-based overloads until .NET Standard 2.1 and .NET Core 2.1.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
    public override
#else
    private
#endif
    async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        await _readLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        var length = Math.Min(buffer.Length, _buffer.Length - _bufferBytesRead);
        _buffer.Slice(_bufferBytesRead, length).CopyTo(buffer);
        _bufferBytesRead += length;

        // Release the write lock if the consumer has finished reading all of
        // the previously written data.
        if (_bufferBytesRead >= _buffer.Length)
        {
            _writeLock.Release();
        }
        // Otherwise, release the read lock again so that the consumer can finish
        // reading the remaining data.
        else
        {
            _readLock.Release();
        }

        return length;
    }

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
    public override async Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) => await WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);

    public override async Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) => await ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
#endif

    public async Task CloseAsync(CancellationToken cancellationToken = default) =>
        // Write an empty buffer that will make ReadAsync(...) return 0, which signifies the end of stream
        await WriteAsync(ReadOnlyMemory<byte>.Empty, cancellationToken).ConfigureAwait(false);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _readLock.Dispose();
            _writeLock.Dispose();
        }

        base.Dispose(disposing);
    }
}

// Unused members that we're forced to implement
internal partial class SimplexStream
{
    [ExcludeFromCodeCoverage]
    public override bool CanRead => true;

    [ExcludeFromCodeCoverage]
    public override bool CanSeek => false;

    [ExcludeFromCodeCoverage]
    public override bool CanWrite => true;

    [ExcludeFromCodeCoverage]
    public override long Position
    {
        get => _bufferBytesRead;
        set => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    public override long Length => _buffer.Length;

    [ExcludeFromCodeCoverage]
    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    [ExcludeFromCodeCoverage]
    public override void Write(byte[] buffer, int offset, int count) =>
        WriteAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    [ExcludeFromCodeCoverage]
    public override void Flush() => throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    [ExcludeFromCodeCoverage]
    public override void SetLength(long value) => throw new NotSupportedException();
}
