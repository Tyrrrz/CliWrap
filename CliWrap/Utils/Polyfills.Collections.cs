// ReSharper disable CheckNamespace
// ReSharper disable RedundantUsingDirective
// ReSharper disable PartialTypeWithSinglePart

using System.Collections.Generic;

#if NET462 || NETSTANDARD2_0
internal static class CollectionPolyfills
{
    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> pair,
        out TKey key,
        out TValue value)
    {
        key = pair.Key;
        value = pair.Value;
    }
}
#endif