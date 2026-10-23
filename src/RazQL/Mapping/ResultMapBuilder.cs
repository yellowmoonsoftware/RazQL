using System.Linq.Expressions;
using System.Reflection;
using RazQL.Internal;

namespace RazQL.Binding;

public abstract record Relation(
    string Column,
    Type OwningType,
    Type ChildType,
    PropertyInfo PropertyInfo,
    IReadOnlyList<Relation> ChildRelations)
{
    public abstract bool ApplyRelation(object owner, object? target);
};

public record AssociationRelation(
    string Column,
    Type OwningType,
    Type ChildType,
    PropertyInfo PropertyInfo,
    IReadOnlyList<Relation> ChildRelations)
    : Relation(Column, OwningType, ChildType, PropertyInfo, ChildRelations)
{
    public override bool ApplyRelation(object owner, object? target)
    {
        PropertyInfo.SetValue(owner, target);
        return true;
    }
}

public record CollectionRelation(
    string Column,
    bool AddNullChildren,
    Type OwningType,
    Type ChildType,
    PropertyInfo PropertyInfo,
    MethodInfo MethodInfo,
    IReadOnlyList<Relation> ChildRelations)
    : Relation(Column, OwningType, ChildType, PropertyInfo, ChildRelations)
{
    public override bool ApplyRelation(object owner, object? target)
    {
        if (target == null && !AddNullChildren)
        {
            // Ensures we don't get called again for a null child
            return true;
        }

        var collection = PropertyInfo.GetValue(owner);
        if (collection != null)
        {
            MethodInfo.Invoke(collection, [target]);
        }
        // If the target was non-null, don't call again for this specific object,
        // or (target was null and) we want to add null children)
        return target != null || AddNullChildren;
    }
}

public record ResultMap<TResult>(
    IReadOnlyList<Relation> Relations,
    IReadOnlyDictionary<Type, Func<object, object?>> IdentitySelectors)
{
    public Type ResultType => typeof(TResult);
    public bool HasMappedAssociations { get; } = Relations.Any();

    public static ResultMap<TResult> Create(Action<IResultMapBuilder<TResult>> action)
    {
        var builder = new ResultMapBuilder<TResult>();
        action(builder);
        return builder.Build();
    }
};

class Indexer(int initial = 0)
{
    public int Value { get; private set; } = initial;

    public int GetValueThenIncrement(int amount = 1)
    {
        var value = Value;
        Value += amount;
        return value;
    }
}

public class ResultMapBuilder<TResult> : IResultMapBuilder<TResult>
{
    private readonly List<Relation> _relations = new();
    private readonly Dictionary<Type, Func<object, object?>> _identitySelectors;

    /// <summary>Creates an independent result map and query-wide identity registry.</summary>
    public ResultMapBuilder() : this(new Dictionary<Type, Func<object, object?>>())
    {
    }

    private ResultMapBuilder(Dictionary<Type, Func<object, object?>> identitySelectors)
    {
        _identitySelectors = identitySelectors;
    }

    /// <inheritdoc />
    public IResultMapBuilder<TResult> Identity<TKey>(Func<TResult, TKey> identitySelector)
    {
        ArgumentNullException.ThrowIfNull(identitySelector);

        if (!_identitySelectors.TryAdd(typeof(TResult), value => identitySelector((TResult)value)))
        {
            throw new InvalidOperationException(
                $"An identity selector is already registered for mapped type '{typeof(TResult)}'. " +
                "Register it once; it applies to every occurrence of that type in the query.");
        }

        return this;
    }
    
    public IResultMapBuilder<TResult> Association<TEntity>(string column, Expression<Func<TResult, TEntity>> associationSelector, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class?
    {
        // build any relationships on target 
        var entityMapBuilder = new ResultMapBuilder<TEntity>(_identitySelectors);
        buildMap?.Invoke(entityMapBuilder);
        var entityResultMap = entityMapBuilder.Build();

        var propertyInfo = associationSelector.GetPropertyInfo();
        var relation = new AssociationRelation(column, typeof(TResult), typeof(TEntity), propertyInfo, entityResultMap.Relations);
        _relations.Add(relation);

        return this;
    }

    public IResultMapBuilder<TResult> Collection<TEntity>(string column, Expression<Func<TResult, ICollection<TEntity>>> collectionSelector, bool addNullChildren, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class?
    {
        // build any relationships on target 
        var entityMapBuilder = new ResultMapBuilder<TEntity>(_identitySelectors);
        buildMap?.Invoke(entityMapBuilder);
        var entityResultMap = entityMapBuilder.Build();

        var (propertyInfo, methodInfo) = collectionSelector.GetPropertyAddMethod();
        var relation = new CollectionRelation(column, addNullChildren, typeof(TResult), typeof(TEntity), propertyInfo, methodInfo, entityResultMap.Relations);
        _relations.Add(relation);

        return this;
    }

    public ResultMap<TResult> Build()
    {
        return new ResultMap<TResult>([.. _relations],
            new Dictionary<Type, Func<object, object?>>(_identitySelectors).AsReadOnly());
    }
}
