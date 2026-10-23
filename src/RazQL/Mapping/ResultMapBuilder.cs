using System.Linq.Expressions;
using System.Reflection;
using RazQL.Internal;

namespace RazQL.Binding;

/// <summary>Describes a property relationship between two mapped object types.</summary>
/// <param name="Column">The child's first column, used as a split boundary.</param>
/// <param name="OwningType">The type containing the relationship property.</param>
/// <param name="ChildType">The child object type.</param>
/// <param name="PropertyInfo">The property receiving the child or containing the child collection.</param>
/// <param name="ChildRelations">The child's relationships, in column-group order.</param>
public abstract record Relation(
    string Column,
    Type OwningType,
    Type ChildType,
    PropertyInfo PropertyInfo,
    IReadOnlyList<Relation> ChildRelations)
{
    /// <summary>Assigns or adds a child to an owning object.</summary>
    /// <param name="owner">The canonical owning object.</param>
    /// <param name="target">The canonical child, or null for a missing child.</param>
    /// <returns>Whether this owner-child application may be recorded as handled.</returns>
    public abstract bool ApplyRelation(object owner, object? target);
};

/// <summary>Assigns a mapped child to a single-object property.</summary>
/// <param name="Column">The child's first column, used as a split boundary.</param>
/// <param name="OwningType">The type containing the relationship property.</param>
/// <param name="ChildType">The child object type.</param>
/// <param name="PropertyInfo">The child property, which must have a setter.</param>
/// <param name="ChildRelations">The child's nested relationships.</param>
public record AssociationRelation(
    string Column,
    Type OwningType,
    Type ChildType,
    PropertyInfo PropertyInfo,
    IReadOnlyList<Relation> ChildRelations)
    : Relation(Column, OwningType, ChildType, PropertyInfo, ChildRelations)
{
    /// <inheritdoc />
    public override bool ApplyRelation(object owner, object? target)
    {
        PropertyInfo.SetValue(owner, target);
        return true;
    }
}

/// <summary>Adds mapped children to a collection property, creating the collection when needed.</summary>
/// <param name="Column">The child's first column, used as a split boundary.</param>
/// <param name="AddNullChildren">Whether a missing child may be added as null.</param>
/// <param name="OwningType">The type containing the collection property.</param>
/// <param name="ChildType">The collection element type.</param>
/// <param name="PropertyInfo">The collection property, initialized by the DTO or the factory.</param>
/// <param name="MethodInfo">The collection's add method.</param>
/// <param name="ChildRelations">The child's nested relationships.</param>
/// <param name="CollectionFactory">Creates a collection to assign when the property is null, or null to require an existing collection.</param>
public record CollectionRelation(
    string Column,
    bool AddNullChildren,
    Type OwningType,
    Type ChildType,
    PropertyInfo PropertyInfo,
    MethodInfo MethodInfo,
    IReadOnlyList<Relation> ChildRelations,
    Func<object>? CollectionFactory = null)
    : Relation(Column, OwningType, ChildType, PropertyInfo, ChildRelations)
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">The collection is null without a factory, or the factory cannot initialize the property with a compatible non-null collection.</exception>
    public override bool ApplyRelation(object owner, object? target)
    {
        var collection = PropertyInfo.GetValue(owner);
        if (collection == null)
        {
            var propertyName = $"{OwningType.FullName}.{PropertyInfo.Name}";
            if (CollectionFactory == null)
            {
                throw new InvalidOperationException(
                    $"Collection property '{propertyName}' is null. Initialize it or register a collection factory.");
            }

            if (PropertyInfo.SetMethod == null)
            {
                throw new InvalidOperationException(
                    $"Collection property '{propertyName}' is null and has no setter to assign the factory-created collection.");
            }

            collection = CollectionFactory();
            if (collection == null || !PropertyInfo.PropertyType.IsInstanceOfType(collection))
            {
                throw new InvalidOperationException(
                    $"The collection factory for '{propertyName}' must return a non-null collection assignable to '{PropertyInfo.PropertyType}'.");
            }

            PropertyInfo.SetValue(owner, collection);
        }

        if (target == null && !AddNullChildren)
        {
            // Ensures we don't get called again for a null child
            return true;
        }

        MethodInfo.Invoke(collection, [target]);
        // If the target was non-null, don't call again for this specific object,
        // or (target was null and) we want to add null children)
        return target != null || AddNullChildren;
    }
}

/// <summary>Contains a result's relationship tree and identities shared throughout one query.</summary>
/// <typeparam name="TResult">The root row-object type, rather than a sequence of row objects.</typeparam>
/// <param name="Relations">The root object's relationships, in column-group order.</param>
/// <param name="IdentitySelectors">Identity providers keyed by mapped object type.</param>
public record ResultMap<TResult>(
    IReadOnlyList<Relation> Relations,
    IReadOnlyDictionary<Type, Func<object, object?>> IdentitySelectors)
{
    /// <summary>Gets the root row-object type.</summary>
    public Type ResultType => typeof(TResult);
    /// <summary>Gets whether any associations or collections are configured.</summary>
    public bool HasMappedAssociations { get; } = Relations.Any();

    /// <summary>Builds a result map using a new, independent builder.</summary>
    /// <param name="action">Configures the root identity and relationship tree.</param>
    /// <returns>The configured result map.</returns>
    public static ResultMap<TResult> Create(Action<IResultMapBuilder<TResult>> action)
    {
        var builder = new ResultMapBuilder<TResult>();
        action(builder);
        return builder.Build();
    }
};

/// <summary>Collects relationships and snapshots identities shared by nested builders.</summary>
/// <typeparam name="TResult">The object type configured by this builder.</typeparam>
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
    
    /// <inheritdoc />
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

    /// <inheritdoc />
    public IResultMapBuilder<TResult> Collection<TEntity>(string column, Expression<Func<TResult, ICollection<TEntity>?>> collectionSelector, bool addNullChildren,
        Action<IResultMapBuilder<TEntity>>? buildMap = null, Func<ICollection<TEntity>>? collectionFactory = null) where TEntity : class?
    {
        // build any relationships on target 
        var entityMapBuilder = new ResultMapBuilder<TEntity>(_identitySelectors);
        buildMap?.Invoke(entityMapBuilder);
        var entityResultMap = entityMapBuilder.Build();

        var (propertyInfo, methodInfo) = collectionSelector.GetPropertyAddMethod();
        var relation = new CollectionRelation(column, addNullChildren, typeof(TResult), typeof(TEntity), propertyInfo, methodInfo,
            entityResultMap.Relations, collectionFactory == null ? null : () => collectionFactory());
        _relations.Add(relation);

        return this;
    }

    /// <summary>Snapshots the current relationships and query-wide identity providers.</summary>
    /// <returns>A result map independent of subsequent builder changes.</returns>
    public ResultMap<TResult> Build()
    {
        return new ResultMap<TResult>([.. _relations],
            new Dictionary<Type, Func<object, object?>>(_identitySelectors).AsReadOnly());
    }
}
