using System.Linq;

namespace CliWrap.Internal.Extensions
{
    internal static class CollectionExtensions
    {
        public static T[] Offset<T>(this T[] arr, int count) =>
            count > 0
                ? arr.Skip(count).ToArray()
                : arr;

        public static T[] Trim<T>(this T[] arr, int count) =>
            arr.Length > count
                ? arr.Take(count).ToArray()
                : arr;
    }
}