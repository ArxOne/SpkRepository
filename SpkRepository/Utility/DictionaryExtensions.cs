using System.Collections.Generic;

namespace ArxOne.Synology.Utility;

internal static class DictionaryExtensions
{
    public static TValue TryGetOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key)
    {
        if(dictionary.TryGetValue(key, out var value))
            return value;
        return default;
    }
}