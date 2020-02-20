using System.IO;

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
    }
}