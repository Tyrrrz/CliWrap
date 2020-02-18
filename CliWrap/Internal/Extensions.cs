using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static MemoryStream ToStream(this byte[] data)
        {
            var stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        public static async Task CopyToAsync(this StreamReader reader, StringBuilder destination,
            CancellationToken cancellationToken = default)
        {
            var buffer = new char[1024 / sizeof(char)];
            int charsRead;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                charsRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                destination.Append(buffer, 0, charsRead);
            } while (charsRead > 0);
        }
    }
}