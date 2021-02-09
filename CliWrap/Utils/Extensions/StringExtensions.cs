using System.Security;

namespace CliWrap.Utils.Extensions
{
    internal static class StringExtensions
    {
        public static SecureString ToSecureString(this string str)
        {
            var secureString = new SecureString();

            foreach (var c in str)
                secureString.AppendChar(c);

            secureString.MakeReadOnly();

            return secureString;
        }
    }
}