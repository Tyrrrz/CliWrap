using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Tests.Internal
{
    internal class UnresolvableEmptyStream : Stream
    {
        private readonly bool _isCancellable;

        public UnresolvableEmptyStream(bool isCancellable = true)
        {
            _isCancellable = isCancellable;
        }

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = false;

        public override long Length { get; } = 0;
        public override long Position { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Thread.Sleep(Timeout.Infinite);
            return 0;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<int>();

            await using var cancellation = _isCancellable
                ? cancellationToken.Register(() => tcs.TrySetCanceled())
                : default;

            return await tcs.Task;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<int>();

            await using var cancellation = _isCancellable
                ? cancellationToken.Register(() => tcs.TrySetCanceled())
                : default;

            return await tcs.Task;
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}