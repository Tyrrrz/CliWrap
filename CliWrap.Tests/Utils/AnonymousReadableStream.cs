using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Tests.Utils;

internal class AnonymousReadableStream : Stream
{
    private readonly Func<Memory<byte>, CancellationToken, Task<int>> _readAsync;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public AnonymousReadableStream(Func<Memory<byte>, CancellationToken, Task<int>> readAsync) =>
        _readAsync = readAsync;

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default) =>
        await _readAsync(buffer, cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Flush() => throw new NotSupportedException();
}