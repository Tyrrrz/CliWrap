using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils
{
    internal class SimplexStream : Stream
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);
        private readonly SemaphoreSlim _readLock = new(0, 1);

        private byte[] _currentBuffer = Array.Empty<byte>();
        private int _currentBufferBytes;
        private int _currentBufferBytesRead;

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

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            await _readLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Take a portion of the buffer that the consumer is interested in
            var length = Math.Min(count, _currentBufferBytes - _currentBufferBytesRead);
            Array.Copy(_currentBuffer, _currentBufferBytesRead, buffer, offset, length);

            // If the consumer finished reading current buffer - release write lock
            if ((_currentBufferBytesRead += count) >= _currentBufferBytes)
            {
                _writeLock.Release();
            }
            // Otherwise - release read lock again so that they can continue reading
            else
            {
                _readLock.Release();
            }

            return length;
        }

        public override async Task WriteAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);

            // Attempt to reuse existing buffer as long as it has enough capacity
            if (_currentBuffer.Length < count)
                _currentBuffer = new byte[count];

            Array.Copy(buffer, offset, _currentBuffer, 0, count);
            _currentBufferBytes = count;
            _currentBufferBytesRead = 0;
            _readLock.Release();
        }

        public async Task ReportCompletionAsync(CancellationToken cancellationToken = default)
        {
            // Write empty buffer that will make ReadAsync return 0, which signifies end-of-stream
            await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            _currentBuffer = Array.Empty<byte>();
            _currentBufferBytes = 0;
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