using System.Security;

namespace CliWrap.Utils.Extensions;

internal static class StringExtensions
{
    extension(string str)
    {
        public SecureString ToSecureString()
        {
            var secureString = new SecureString();

            foreach (var c in str)
                secureString.AppendChar(c);

            secureString.MakeReadOnly();

            return secureString;
        }
    }
}
