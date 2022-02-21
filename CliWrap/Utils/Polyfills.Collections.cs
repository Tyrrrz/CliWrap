// ReSharper disable CheckNamespace

#if NET461 || NETSTANDARD2_0
using System.Collections.Generic;

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