using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static async Task<string> ReadToEndAsync(this StreamReader streamReader,
            CancellationToken cancellationToken)
        {
            var sb = new StringBuilder();
            var buffer = new char[1024];
            int charsRead;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                charsRead = await streamReader.ReadAsync(buffer, 0, buffer.Length);
                sb.Append(buffer, 0, charsRead);
            } while (charsRead > 0);

            return sb.ToString();
        }

        public static bool TryKill(this Process process)
        {
            try
            {
                process?.Kill();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}