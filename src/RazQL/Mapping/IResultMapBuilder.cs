using System.Linq.Expressions;

namespace RazQL.Binding;

/// <summary>Describes a result object's identity and relationships to other mapped objects.</summary>
/// <typeparam name="TResult">The object type configured by this builder.</typeparam>
public interface IResultMapBuilder<TResult>
{
    /// <summary>Defines the identity used for every occurrence of this result type in the query.</summary>
    /// <typeparam name="TKey">The identity type, including tuple types for composite keys.</typeparam>
    /// <param name="identitySelector">Selects an identity whose equality and hash code remain stable during mapping.</param>
    /// <returns>This builder.</returns>
    /// <remarks>
    /// Register a selector once per mapped type. Types without a selector use their own equality and hash code.
    /// Selectors are not invoked for null objects.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The selector is null.</exception>
    /// <exception cref="InvalidOperationException">This query already has a selector for this mapped type.</exception>
    IResultMapBuilder<TResult> Identity<TKey>(Func<TResult, TKey> identitySelector);

    /// <summary>Maps a child object to a property and optionally describes that child's relationships.</summary>
    /// <typeparam name="TEntity">The child object type.</typeparam>
    /// <param name="column">The first column of the child's contiguous result-column group, used as a Dapper split boundary.</param>
    /// <param name="associationSelector">Selects the child property on the current object.</param>
    /// <param name="buildMap">An optional callback configuring the child's identity and relationships.</param>
    /// <returns>This builder.</returns>
    /// <remarks>Every mapped value is assigned, including null. The last assignment wins; association conflicts are not tracked.</remarks>
    public IResultMapBuilder<TResult> Association<TEntity>(string column,
        Expression<Func<TResult, TEntity>> associationSelector, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class?;

    /// <summary>Maps child objects into a collection property, optionally initializing it through a factory.</summary>
    /// <typeparam name="TEntity">The collection element type.</typeparam>
    /// <param name="column">The first column of the child's contiguous result-column group, used as a Dapper split boundary.</param>
    /// <param name="resultSelector">Selects the collection property on the current object.</param>
    /// <param name="addNullChildren">Whether a missing child may be added as a null collection entry.</param>
    /// <param name="buildMap">An optional callback configuring the child's identity and relationships.</param>
    /// <param name="collectionFactory">Creates a collection to assign when the property is null. The property must have a setter when initialization is needed, and the returned collection must be assignable to its declared type.</param>
    /// <returns>This builder.</returns>
    public IResultMapBuilder<TResult> Collection<TEntity>(string column,
        Expression<Func<TResult, ICollection<TEntity>?>> resultSelector, bool addNullChildren,
        Action<IResultMapBuilder<TEntity>>? buildMap = null, Func<ICollection<TEntity>>? collectionFactory = null) where TEntity : class?;
    
    /// <summary>Maps non-null child objects into a collection property, optionally initializing it through a factory.</summary>
    /// <typeparam name="TEntity">The collection element type.</typeparam>
    /// <param name="column">The first column of the child's contiguous result-column group, used as a Dapper split boundary.</param>
    /// <param name="resultSelector">Selects the collection property on the current object.</param>
    /// <param name="buildMap">An optional callback configuring the child's identity and relationships.</param>
    /// <param name="collectionFactory">Creates a collection to assign when the property is null. The property must have a setter when initialization is needed, and the returned collection must be assignable to its declared type.</param>
    /// <returns>This builder.</returns>
    public IResultMapBuilder<TResult> Collection<TEntity>(string column,
        Expression<Func<TResult, ICollection<TEntity>?>> resultSelector,
        Action<IResultMapBuilder<TEntity>>? buildMap = null, Func<ICollection<TEntity>>? collectionFactory = null) where TEntity : class? =>
        Collection(column, resultSelector, false, buildMap, collectionFactory);
}
