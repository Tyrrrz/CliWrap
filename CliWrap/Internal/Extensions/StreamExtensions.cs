using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("CliWrap.Tests")]
namespace CliWrap.Internal.Extensions
{
    internal static class StreamExtensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, bool autoFlush,
            CancellationToken cancellationToken = default)
        {
            using var buffer = PooledBuffer.ForStream();

            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer.Array, cancellationToken)) != 0)
            {
                await destination.WriteAsync(buffer.Array, 0, bytesRead, cancellationToken);

                if (autoFlush)
                    await destination.FlushAsync(cancellationToken);
            }
        }

        public static async IAsyncEnumerable<string> ReadAllLinesAsync(this StreamReader reader,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var stringBuilder = new StringBuilder();
            using var buffer = PooledBuffer.ForStreamReader();
            
            // contains the first char of the line break string in a series of line breaks
            // this enables us to convert all of these: "A\r\rB", "A\n\nB" and "A\r\n\r\nB" into 3 lines: "A", "" and "B"
            // if we didn't do this the last example would be converted into 5 lines
            char? lineSeparator = null;
            
            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer.Array, cancellationToken)) > 0)
            {
                for (var i = 0; i < charsRead; i++)
                {
                    char current = buffer.Array[i];

                    // if the first char we read is a '\r' we don't return an empty line 
                    if (current == '\r' && reader.BaseStream.Position == charsRead && i == 0)
                    {
                        continue;
                    }
                    
                    if (current == '\n' || current == '\r')
                    {
                        lineSeparator ??= current;
                        
                        if (current == lineSeparator)
                        {
                            // Trigger on buffered input (even if it's empty)
                            yield return stringBuilder.ToString();
                            stringBuilder.Clear();
                        }
                    }
                    else
                    {
                        stringBuilder.Append(current);
                        lineSeparator = null;
                    }
                }
            }

            // Yield what's remaining
            if (stringBuilder.Length > 0)
                yield return stringBuilder.ToString();
        }
    }
}