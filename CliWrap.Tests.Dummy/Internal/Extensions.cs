using System;
using System.Text;

namespace CliWrap.Tests.Dummy.Internal
{
    internal static class Extensions
    {
        public static string NextString(this Random random, int length)
        {
            var source = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890"
                .ToCharArray();

            var buffer = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                var c = source[random.Next(source.Length)];
                buffer.Append(c);
            }

            return buffer.ToString();
        }
    }
}