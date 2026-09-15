namespace RazQL.Internal;

internal static class CollectionExtensions
{
    public static void DoIfExists<TKey, TValue, TContext>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey? key,
        Action<TKey, TValue, TContext> action, TContext context)
        where TKey : struct, Enum
    {
        if (!key.HasValue) return;

        action(key.Value, dictionary[key.Value], context);
    }

    public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> factory)
    {
        if (dictionary.TryGetValue(key, out var existingVal)) return existingVal;
        var value = factory(key);
        dictionary[key] = value;
        return value;
    }
}
