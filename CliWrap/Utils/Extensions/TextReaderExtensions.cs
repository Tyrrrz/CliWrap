using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CliWrap.Utils.Extensions;

internal static class TextReaderExtensions
{
    extension(TextReader reader)
    {
        public async IAsyncEnumerable<string> ReadAllLinesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default
        )
        {
            while (true)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null)
                    yield break;

                yield return line;
            }
        }
    }
}
