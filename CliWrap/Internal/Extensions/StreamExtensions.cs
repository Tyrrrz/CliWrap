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
            using var buffer = PooledBuffer.ForStream();

            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer.Array, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer.Array, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                if (autoFlush)
                    await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public static async IAsyncEnumerable<string> ReadAllLinesAsync(this StreamReader reader,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stringBuilder = new StringBuilder();
            using var buffer = PooledBuffer.ForStreamReader();

            // Following sequences are treated as individual linebreaks:
            // - \r
            // - \n
            // - \r\n
            // Even though \r and \n are linebreaks on their own, \r\n together
            // should not yield two lines. To ensure that, we keep track of the
            // previous char and check if it's part of a sequence.
            var prevSeqChar = (char?) null;

            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer.Array, cancellationToken).ConfigureAwait(false)) > 0)
            {
                for (var i = 0; i < charsRead; i++)
                {
                    var curChar = buffer.Array[i];

                    // If current char and last char are part of a line break sequence,
                    // skip over the current char and move on.
                    // The buffer was already yielded in the previous iteration, so there's
                    // nothing left to do.
                    if (prevSeqChar == '\r' && curChar == '\n')
                    {
                        prevSeqChar = null;
                        continue;
                    }

                    // If current char is \n or \r, yield the buffer (even if it is empty)
                    if (curChar == '\n' || curChar == '\r')
                    {
                        yield return stringBuilder.ToString();
                        stringBuilder.Clear();
                    }
                    // For any other char, just append it to the buffer
                    else
                    {
                        stringBuilder.Append(curChar);
                    }

                    prevSeqChar = curChar;
                }
            }

            // Yield what's remaining in the buffer
            if (stringBuilder.Length > 0)
                yield return stringBuilder.ToString();
        }
    }
}