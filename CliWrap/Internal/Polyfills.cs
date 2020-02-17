// ReSharper disable CheckNamespace

#if !NETSTANDARD2_1
using System.Threading;
using System.Threading.Tasks;

namespace System.IO
{
    internal static class Extensions
    {
        public static async Task CopyToAsync(this Stream source, Stream destination, CancellationToken cancellationToken)
        {
            var buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            }
        }

        public static ValueTask DisposeAsync(this StreamWriter writer)
        {
            writer.Dispose();
            return default;
        }
    }
}
#endif