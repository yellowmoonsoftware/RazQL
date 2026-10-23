namespace RazQL.Dapper.Mapping;

/// <summary>Resolves one canonical object per mapped type and identity within a query execution.</summary>
/// <param name="identitySelectors">Identity providers, with DTO equality used for unconfigured types.</param>
internal class ObjectCache(IReadOnlyDictionary<Type, Func<object, object?>> identitySelectors)
{
    /// <summary>Retains a materialized object while comparing its captured identity and mapped type.</summary>
    /// <param name="Value">The original object, or null for a missing object.</param>
    /// <param name="ObjectType">The mapped object type.</param>
    /// <param name="Identity">The identity captured at key construction.</param>
    public record CacheKey(object? Value, Type ObjectType, object? Identity)
    {
        /// <summary>Validates the mapped type and evaluates the identity provider once for a non-null object.</summary>
        /// <param name="value">The materialized object.</param>
        /// <param name="expectedType">The declared mapped type.</param>
        /// <param name="identityProvider">The configured selector, or the identity function for DTO equality.</param>
        /// <returns>A key retaining both the real object and its captured identity.</returns>
        public static CacheKey For(object? value, Type expectedType, Func<object, object?> identityProvider)
        {
            if (value != null && value.GetType() != expectedType)
            {
                throw new ArgumentException("Candidate type does not match expected type");
            }

            var identity = value != null ? identityProvider(value) : null;
            return new CacheKey(value, expectedType, identity);
        }

        /// <summary>Compares mapped types and captured identities, distinguishing missing objects from null identities.</summary>
        /// <param name="other">The key to compare.</param>
        /// <returns>Whether both keys identify the same mapped object.</returns>
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

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(ObjectType, Identity);
    }

    /// <summary>Wraps a canonical object and its applied-owner tracking, including for null objects.</summary>
    /// <param name="Value">The canonical object.</param>
    /// <param name="ObjectType">The mapped object type.</param>
    /// <param name="AssocOwners">Canonical owner wrappers compared by reference.</param>
    public record CacheValue(object? Value, Type ObjectType, HashSet<CacheValue> AssocOwners)
    {
        /// <summary>Wraps the key's real object with independent owner tracking.</summary>
        /// <param name="key">The key for a newly encountered identity.</param>
        /// <returns>The canonical wrapper to store in the cache.</returns>
        public static CacheValue ForKey(CacheKey key) =>
            new(key.Value, key.ObjectType, new(ReferenceEqualityComparer.Instance));
    }

    private readonly Dictionary<CacheKey, CacheValue> _cache = new();

    /// <summary>Constructs an identity key and resolves its canonical object.</summary>
    /// <param name="candidate">The materialized candidate object.</param>
    /// <param name="expectedType">The mapped object type.</param>
    /// <returns>The canonical wrapper and whether it was newly cached.</returns>
    public (CacheValue, bool) Get(object? candidate, Type expectedType)
    {
        var key = CacheKey.For(candidate, expectedType, identitySelectors.GetValueOrDefault(expectedType, value => value));
        return Get(key);
    }

    /// <summary>Resolves a prepared key, preserving the first object encountered for that identity.</summary>
    /// <param name="key">The prepared identity key.</param>
    /// <returns>The canonical wrapper and whether it was newly cached.</returns>
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
