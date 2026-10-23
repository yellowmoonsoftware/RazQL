using RazQL.Dapper.Mapping;

namespace RazQL.Dapper.Tests.Mapping;

public class ObjectCacheTests
{
    [Fact]
    public void Get_UsesDtoEqualityWhenNoSelectorIsConfigured()
    {
        var cache = new ObjectCache(new Dictionary<Type, Func<object, object?>>());
        var candidate = new ValueEntity(1);

        var (first, firstIsNew) = cache.Get(candidate, typeof(ValueEntity));
        var (second, secondIsNew) = cache.Get(new ValueEntity(1), typeof(ValueEntity));

        Assert.True(firstIsNew);
        Assert.False(secondIsNew);
        Assert.Same(first, second);
        Assert.Same(candidate, second.Value);
    }

    [Fact]
    public void Get_UsesReferenceEqualityForOrdinaryDtosWithoutASelector()
    {
        var cache = new ObjectCache(new Dictionary<Type, Func<object, object?>>());

        var (first, _) = cache.Get(new Entity { Id = 1 }, typeof(Entity));
        var (second, isNew) = cache.Get(new Entity { Id = 1 }, typeof(Entity));

        Assert.True(isNew);
        Assert.NotSame(first, second);
    }

    [Fact]
    public void Get_UsesSelectedIdentityAndPreservesTheFirstObject()
    {
        var cache = CreateCache<Entity, int>(entity => entity.Id);
        var candidate = new Entity { Id = 1, Name = "first" };
        var (first, _) = cache.Get(candidate, typeof(Entity));

        var (second, isNew) = cache.Get(new Entity { Id = 1, Name = "second" }, typeof(Entity));

        Assert.False(isNew);
        Assert.Same(first, second);
        Assert.Same(candidate, second.Value);
        Assert.Equal("first", Assert.IsType<Entity>(second.Value).Name);
    }

    [Fact]
    public void Get_AcceptsOriginalObjectKeysWithCustomIdentity()
    {
        var selectors = new Dictionary<Type, Func<object, object?>>
        {
            [typeof(Entity)] = value => ((Entity)value).Id
        };
        var cache = new ObjectCache(selectors);
        var candidate = new Entity { Id = 1 };
        var key = ObjectCache.CacheKey.For(candidate, typeof(Entity), selectors[typeof(Entity)]);

        var (first, _) = cache.Get(key);
        var (second, isNew) = cache.Get(ObjectCache.CacheKey.For(new Entity { Id = 1 }, typeof(Entity), selectors[typeof(Entity)]));

        Assert.Same(candidate, key.Value);
        Assert.Same(candidate, first.Value);
        Assert.Equal(typeof(Entity), first.ObjectType);
        Assert.Same(first, second);
        Assert.False(isNew);
    }

    [Fact]
    public void CacheKeyFor_EvaluatesSelectorOnlyDuringConstruction()
    {
        var calls = 0;
        var selectors = new Dictionary<Type, Func<object, object?>>
        {
            [typeof(Entity)] = value =>
            {
                calls++;
                return ((Entity)value).Id;
            }
        };
        var first = ObjectCache.CacheKey.For(new Entity { Id = 1, Name = "first" }, typeof(Entity), selectors[typeof(Entity)]);
        var second = ObjectCache.CacheKey.For(new Entity { Id = 1, Name = "second" }, typeof(Entity), selectors[typeof(Entity)]);
        var cache = new ObjectCache(selectors);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        var (firstValue, _) = cache.Get(first);
        var (secondValue, isNew) = cache.Get(second);
        cache.Get(first);

        Assert.Same(firstValue, secondValue);
        Assert.False(isNew);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Get_EvaluatesSelectorOncePerIncomingKey()
    {
        var calls = 0;
        var cache = CreateCache<Entity, int>(entity =>
        {
            calls++;
            return entity.Id;
        });

        var (first, _) = cache.Get(new Entity { Id = 1 }, typeof(Entity));
        var (second, _) = cache.Get(new Entity { Id = 1 }, typeof(Entity));

        Assert.Same(first, second);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Get_DistinguishesUnequalIdentitiesWithTheSameHashCode()
    {
        var cache = CreateCache<Entity, CollisionKey>(entity => new CollisionKey(entity.Id));
        var (first, _) = cache.Get(new Entity { Id = 1 }, typeof(Entity));

        var (different, differentIsNew) = cache.Get(new Entity { Id = 2 }, typeof(Entity));
        var (equal, equalIsNew) = cache.Get(new Entity { Id = 1 }, typeof(Entity));

        Assert.NotSame(first, different);
        Assert.True(differentIsNew);
        Assert.Same(first, equal);
        Assert.False(equalIsNew);
    }

    [Fact]
    public void Get_DistinguishesCompositeIdentities()
    {
        var cache = CreateCache<Entity, (int, int)>(entity => (entity.TenantId, entity.Id));
        var (first, _) = cache.Get(new Entity { TenantId = 1, Id = 2 }, typeof(Entity));

        var (equal, _) = cache.Get(new Entity { TenantId = 1, Id = 2 }, typeof(Entity));
        var (different, _) = cache.Get(new Entity { TenantId = 2, Id = 2 }, typeof(Entity));

        Assert.Same(first, equal);
        Assert.NotSame(first, different);
    }

    [Fact]
    public void Get_KeepsTypesWithEqualIdentitiesSeparate()
    {
        var cache = new ObjectCache(new Dictionary<Type, Func<object, object?>>
        {
            [typeof(Entity)] = value => ((Entity)value).Id,
            [typeof(ValueEntity)] = value => ((ValueEntity)value).Id
        });

        var (first, _) = cache.Get(new Entity { Id = 1 }, typeof(Entity));
        var (second, _) = cache.Get(new ValueEntity(1), typeof(ValueEntity));

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Get_DoesNotInvokeSelectorForNullObjects()
    {
        var cache = CreateCache<Entity, int>(_ => throw new InvalidOperationException());

        var (first, firstIsNew) = cache.Get(null, typeof(Entity));
        var (second, secondIsNew) = cache.Get(null, typeof(Entity));

        Assert.Null(first.Value);
        Assert.True(firstIsNew);
        Assert.False(secondIsNew);
        Assert.Same(first, second);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Get_DistinguishesMissingObjectsFromObjectsWithNullIdentities(bool missingFirst)
    {
        var cache = CreateCache<Entity, string?>(_ => null);
        cache.Get(missingFirst ? null : new Entity(), typeof(Entity));

        var (missing, _) = cache.Get(null, typeof(Entity));
        var (present, _) = cache.Get(new Entity(), typeof(Entity));
        var (equal, _) = cache.Get(new Entity(), typeof(Entity));
        var (missingAgain, _) = cache.Get(null, typeof(Entity));

        Assert.NotSame(missing, present);
        Assert.Null(missing.Value);
        Assert.NotNull(present.Value);
        Assert.Same(present, equal);
        Assert.Same(missing, missingAgain);
    }

    [Fact]
    public void Get_KeepsNullObjectsOfDifferentTypesSeparate()
    {
        var cache = new ObjectCache(new Dictionary<Type, Func<object, object?>>());

        var (first, _) = cache.Get(null, typeof(Entity));
        var (second, _) = cache.Get(null, typeof(ValueEntity));

        Assert.NotSame(first, second);
    }

    [Fact]
    public void AssocOwners_UsesStableWrapperIdentityWhenDtoHashCodeChanges()
    {
        var cache = CreateCache<MutableEntity, int>(entity => entity.Id);
        var ownerObject = new MutableEntity { Id = 1, Name = "before" };
        var (owner, _) = cache.Get(ownerObject, typeof(MutableEntity));
        var (target, _) = cache.Get(new MutableEntity { Id = 2 }, typeof(MutableEntity));
        var property = typeof(MutableEntity).GetProperty(nameof(MutableEntity.Name))!;
        target.AssocOwners.Add(new ObjectCache.AssociationOwner(owner, property));

        ownerObject.Name = "after";
        var (resolvedOwner, _) = cache.Get(new MutableEntity { Id = 1 }, typeof(MutableEntity));

        Assert.Same(owner, resolvedOwner);
        var context = new ObjectCache.AssociationOwner(resolvedOwner, property);
        Assert.Contains(context, target.AssocOwners);
        Assert.False(target.AssocOwners.Add(context));
    }

    [Fact]
    public void Get_RejectsCandidateOfUnexpectedType()
    {
        var cache = new ObjectCache(new Dictionary<Type, Func<object, object?>>());

        Assert.Throws<ArgumentException>(() => cache.Get(new Entity(), typeof(ValueEntity)));
    }

    private static ObjectCache CreateCache<TEntity, TKey>(Func<TEntity, TKey> selector) =>
        new(new Dictionary<Type, Func<object, object?>>
        {
            [typeof(TEntity)] = value => selector((TEntity)value)
        });

    private sealed class Entity
    {
        public int TenantId { get; init; }
        public int Id { get; init; }
        public string? Name { get; init; }
    }

    private sealed record ValueEntity(int Id);

    private sealed record CollisionKey(int Id)
    {
        public override int GetHashCode() => 0;
    }

    private sealed record MutableEntity
    {
        public int Id { get; init; }
        public string? Name { get; set; }
    }
}
