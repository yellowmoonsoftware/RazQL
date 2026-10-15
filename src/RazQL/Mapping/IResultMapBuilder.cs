using System.Linq.Expressions;

namespace RazQL.Binding;

public interface IResultMapBuilder<TResult>
{
    public IResultMapBuilder<TResult> Association<TEntity>(string column,
        Expression<Func<TResult, TEntity>> associationSelector, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class?;

    public IResultMapBuilder<TResult> Collection<TEntity>(string column,
        Expression<Func<TResult, ICollection<TEntity>>> resultSelector, bool addNullChildren, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class?;
    
    public IResultMapBuilder<TResult> Collection<TEntity>(string column,
        Expression<Func<TResult, ICollection<TEntity>>> resultSelector, Action<IResultMapBuilder<TEntity>>? buildMap = null) where TEntity : class? =>
        Collection(column, resultSelector, false, buildMap);
}