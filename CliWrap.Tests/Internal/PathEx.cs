using System.IO;

namespace CliWrap.Tests.Internal
{
    internal static class PathEx
    {
        public static string NormalizePath(string path) => Path.GetFullPath(path).TrimEnd('\\', '/');
    }
}