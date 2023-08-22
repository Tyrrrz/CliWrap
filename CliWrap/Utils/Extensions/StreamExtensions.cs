using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Utils.Extensions;

internal static class StreamExtensions
{
    public static async Task CopyToAsync(
        this Stream source,
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

    public static async IAsyncEnumerable<string> ReadAllLinesAsync(
        this StreamReader reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        // We could use reader.ReadLineAsync() and loop on it, but that method
        // only supports cancellation on .NET 7+ and it's impossible to polyfill
        // it for non-seekable streams. So we have to do it manually.

        var lineBuffer = new StringBuilder();
        using var buffer = MemoryPool<char>.Shared.Rent(BufferSizes.StreamReader);

        // Following sequences are treated as individual linebreaks:
        // - \r
        // - \n
        // - \r\n
        // Even though \r and \n are linebreaks on their own, \r\n together
        // should not yield two lines.
        var isLastCaretReturn = false;
        while (true)
        {
            var charsRead = await reader
                .ReadAsync(buffer.Memory, cancellationToken)
                .ConfigureAwait(false);
            if (charsRead <= 0)
                break;

            for (var i = 0; i < charsRead; i++)
            {
                var c = buffer.Memory.Span[i];

                // If the current char and the last char are part of a line break sequence,
                // skip over the current char and move on.
                // The buffer was already yielded in the previous iteration, so there's
                // nothing left to do.
                if (isLastCaretReturn && c == '\n')
                {
                    isLastCaretReturn = false;
                    continue;
                }

                // If the current char is \n or \r, yield the buffer (even if it is empty)
                if (c is '\n' or '\r')
                {
                    yield return lineBuffer.ToString();
                    lineBuffer.Clear();
                }
                // For any other char, just append it to the buffer
                else
                {
                    lineBuffer.Append(c);
                }

                isLastCaretReturn = c == '\r';
            }
        }

        // Yield what's remaining in the buffer
        if (lineBuffer.Length > 0)
            yield return lineBuffer.ToString();
    }
}
