// ReSharper disable CheckNamespace

#if !NETSTANDARD2_1
using System.Threading.Tasks;

namespace System.IO
{
    internal static class Extensions
    {
        public static void Test() {}

        public static ValueTask DisposeAsync(this StreamWriter writer)
        {
            writer.Dispose();
            return default;
        }
    }
}
#endif