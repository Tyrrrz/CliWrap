namespace CliWrap.Internal
{
    internal static class Extensions
    {
        public static bool IsBlank(this string str) => string.IsNullOrWhiteSpace(str);

        public static bool IsNotBlank(this string str) => !IsBlank(str);
    }
}