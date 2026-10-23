namespace RazQL.Dapper.Mapping;

internal class ObjectCache(IReadOnlyDictionary<Type, Func<object, object?>> identitySelectors)
{
    public record CacheKey(object? Value, Type ObjectType, object? Identity)
    {
        public static CacheKey For(object? value, Type expectedType, Func<object, object?> identityProvider)
        {
            if (value != null && value.GetType() != expectedType)
            {
                throw new ArgumentException("Candidate type does not match expected type");
            }

            var identity = value != null ? identityProvider(value) : null;
            return new CacheKey(value, expectedType, identity);
        }

        public virtual bool Equals(CacheKey? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null || EqualityContract != other.EqualityContract || ObjectType != other.ObjectType)
            {
                return false;
            }

            if (Value == null || other.Value == null)
            {
                return Value == null && other.Value == null;
            }

            return object.Equals(Identity, other.Identity);
        }

        public override int GetHashCode() => HashCode.Combine(ObjectType, Identity);
    }

    public record CacheValue(object? Value, Type ObjectType, HashSet<CacheValue> AssocOwners)
    {
        public static CacheValue ForKey(CacheKey key) =>
            new(key.Value, key.ObjectType, new(ReferenceEqualityComparer.Instance));
    }

    private readonly Dictionary<CacheKey, CacheValue> _cache = new();

    public (CacheValue, bool) Get(object? candidate, Type expectedType)
    {
        var key = CacheKey.For(candidate, expectedType, identitySelectors.GetValueOrDefault(expectedType, value => value));
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
