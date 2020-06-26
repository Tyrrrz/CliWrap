using System;
using System.IO;

namespace CliWrap.Tests.Internal
{
    internal class RandomStream : Stream
    {
        private readonly Random _random;

        public override bool CanRead { get; } = true;
        public override bool CanSeek { get; } = false;
        public override bool CanWrite { get; } = false;

        public override long Length { get; }
        public override long Position { get; set; }

        public RandomStream(Random random, long length)
        {
            _random = random;
            Length = length;
        }

        public RandomStream(long length)
            : this(new Random(), length)
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesToRead = Length >= 0
                ? (int) Math.Min(count, Length - Position)
                : count;

            _random.NextBytes(buffer.AsSpan(0, bytesToRead));
            Position += bytesToRead;

            return bytesToRead;
        }

        public override void Flush() => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}