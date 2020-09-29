using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal.Extensions
{
    internal static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, bool autoFlush,
            CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(BufferSizes.Stream);
            try
            {
                int bytesRead;

                while ((bytesRead = await source.ReadAsync(buffer, cancellationToken)) != 0)
                {
                    await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);

                    if (autoFlush)
                        await destination.FlushAsync(cancellationToken);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public static async IAsyncEnumerable<string> ReadAllLinesAsync(this StreamReader reader,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var buffer = ArrayPool<char>.Shared.Rent(BufferSizes.StreamReader);
            try
            {
                int charsRead;

                var stringBuilder = new StringBuilder();
                while ((charsRead = await reader.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    for (var i = 0; i < charsRead; i++)
                    {
                        if (buffer[i] == '\n')
                        {
                            // Trigger on buffered input (even if it's empty)
                            yield return stringBuilder.ToString();
                            stringBuilder.Clear();
                        }
                        else if (buffer[i] != '\r')
                        {
                            stringBuilder.Append(buffer[i]);
                        }
                    }
                }

                // Yield what's remaining
                if (stringBuilder.Length > 0)
                    yield return stringBuilder.ToString();
            }
            finally
            {
                ArrayPool<char>.Shared.Return(buffer);
            }
        }
    }
}