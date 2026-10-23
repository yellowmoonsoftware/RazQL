using System.Linq.Expressions;

namespace RazQL.Binding;

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
    IResultMapBuilder<TResult> Identity<TKey>(Func<TResult, TKey> identitySelector);

    public IResultMapBuilder<TResult> Association<TEntity>(string column,
        Expression<Func<TResult, TEntity>> associationSelector, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class?;

    public IResultMapBuilder<TResult> Collection<TEntity>(string column,
        Expression<Func<TResult, ICollection<TEntity>>> resultSelector, bool addNullChildren, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class?;
    
    public IResultMapBuilder<TResult> Collection<TEntity>(string column,
        Expression<Func<TResult, ICollection<TEntity>>> resultSelector, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class? =>
        Collection(column, resultSelector, false, buildMap);
}
