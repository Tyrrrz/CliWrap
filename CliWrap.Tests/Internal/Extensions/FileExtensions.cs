using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Tests.Internal.Extensions
{
    internal static class FileExtensions
    {
        public static async Task WriteAllTextAsync(this FileInfo file, string contents, CancellationToken cancellationToken = default) =>
            await File.WriteAllTextAsync(file.FullName, contents, cancellationToken);
    }
}