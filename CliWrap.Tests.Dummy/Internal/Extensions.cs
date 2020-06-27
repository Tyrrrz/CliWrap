using System;
using System.Text;

namespace CliWrap.Tests.Dummy.Internal
{
    internal static class Extensions
    {
        private static readonly char[] AvailableChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890"
            .ToCharArray();

        public static char NextChar(this Random random) =>
            AvailableChars[random.Next(AvailableChars.Length)];

        public static string NextString(this Random random, int length)
        {
            var buffer = new StringBuilder();

            for (var i = 0; i < length; i++)
                buffer.Append(random.NextChar());

            return buffer.ToString();
        }
    }
}