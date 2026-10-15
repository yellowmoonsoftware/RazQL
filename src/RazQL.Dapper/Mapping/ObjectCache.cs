namespace RazQL.Dapper.Mapping;

internal class ObjectCache
{
    public record CacheKey(object? Value, Type ObjectType)
    {
        public static CacheKey For(object? value, Type expectedType)
        {
            if (value != null && value.GetType() != expectedType)
            {
                throw new ArgumentException("Candidate type does not match expected type");
            }

            return new CacheKey(value, expectedType);
        }
    }

    public record CacheValue(object? Value, Type ObjectType, HashSet<CacheValue> AssocOwners)
    {
        public static CacheValue ForKey(CacheKey key) => new(key.Value, key.ObjectType, new());
    }

    private readonly Dictionary<CacheKey, CacheValue> _cache = new();

    public (CacheValue, bool) Get(object? candidate, Type expectedType)
    {
        var key = CacheKey.For(candidate, expectedType);
        return Get(key);
    }
    
    
    public (CacheValue, bool) Get(CacheKey key) 
    {
        if (_cache.TryGetValue(key, out var value))
        {
            return (value, false);
        }

        var newValue = CacheValue.ForKey(key);
        _cache.Add(key, newValue);
        return (newValue, true);
    }
}