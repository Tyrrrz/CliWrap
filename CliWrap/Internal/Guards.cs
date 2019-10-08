using System;

namespace CliWrap.Internal
{
    internal static class Guards
    {
        public static T GuardNotNull<T>(this T o, string argName = null) where T : class
        {
            return o ?? throw new ArgumentNullException(argName);
        }
    }
}