﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal class HalfDuplexStream : Stream
    {
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _readLock = new SemaphoreSlim(0, 1);

        private byte[] _currentBuffer = Array.Empty<byte>();
        private int _currentBufferBytesRead;

        [ExcludeFromCodeCoverage]
        public override bool CanRead { get; } = true;

        [ExcludeFromCodeCoverage]
        public override bool CanSeek { get; } = false;

        [ExcludeFromCodeCoverage]
        public override bool CanWrite { get; } = true;

        [ExcludeFromCodeCoverage]
        public override long Position { get; set; }

        [ExcludeFromCodeCoverage]
        public override long Length => throw new NotSupportedException();

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _readLock.WaitAsync(cancellationToken);

            // Take a portion of the buffer that the consumer is interested in
            var length = Math.Min(Math.Min(
                count,
                _currentBuffer.Length - _currentBufferBytesRead),
                buffer.Length - offset);
            
            Array.Copy(_currentBuffer, _currentBufferBytesRead, buffer, offset, length);

            // If the consumer finished reading current buffer - release write lock
            if ((_currentBufferBytesRead += count) >= _currentBuffer.Length)
            {
                _writeLock.Release();
            }
            // Otherwise - release read lock so that they can finish reading
            else
            {
                _readLock.Release();
            }

            return length;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _writeLock.WaitAsync(cancellationToken);
            _currentBuffer = buffer.Skip(offset).Take(count).ToArray();
            _currentBufferBytesRead = 0;
            _readLock.Release();
        }

        public async Task ReportCompletionAsync(CancellationToken cancellationToken = default)
        {
            // Write empty buffer that will make ReadAsync return 0, which signifies end-of-stream
            await _writeLock.WaitAsync(cancellationToken);
            _currentBuffer = Array.Empty<byte>();
            _currentBufferBytesRead = 0;
            _readLock.Release();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _readLock.Dispose();
                _writeLock.Dispose();
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
}