using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils.Extensions;

internal static class StreamExtensions
{
    extension(Stream source)
    {
        public async Task CopyToAsync(
            Stream destination,
            bool autoFlush,
            CancellationToken cancellationToken = default
        )
        {
            using var buffer = MemoryPool<byte>.Shared.Rent(BufferSizes.Stream);
            while (true)
            {
                var bytesRead = await source
                    .ReadAsync(buffer.Memory, cancellationToken)
                    .ConfigureAwait(false);

                if (bytesRead <= 0)
                    break;

                await destination
                    .WriteAsync(buffer.Memory[..bytesRead], cancellationToken)
                    .ConfigureAwait(false);

                if (autoFlush)
                    await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
