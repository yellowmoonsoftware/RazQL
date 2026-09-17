using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Generates and executes SQL described by a mapper query.</summary>
public interface IQueryExecutor
{
    /// <summary>Executes a query that returns zero or more mapped rows.</summary>
    /// <typeparam name="TMapper">The mapper interface that owns the query.</typeparam>
    /// <typeparam name="TCriteria">The query criteria type.</typeparam>
    /// <typeparam name="TResult">The row-mapping result type.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="criteria">The criteria passed to the Razor template.</param>
    /// <param name="cancellationToken">A token that cancels SQL generation, connection opening, or query execution.</param>
    /// <returns>A task containing the mapped rows.</returns>
    Task<IEnumerable<TResult>> ExecuteAsync<TMapper, TCriteria, TResult>(QueryDescriptor<TMapper, TCriteria, IEnumerable<TResult>> descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>Executes a query that returns zero or one mapped row.</summary>
    /// <typeparam name="TMapper">The mapper interface that owns the query.</typeparam>
    /// <typeparam name="TCriteria">The query criteria type.</typeparam>
    /// <typeparam name="TResult">The row-mapping result type.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="criteria">The criteria passed to the Razor template.</param>
    /// <param name="cancellationToken">A token that cancels SQL generation, connection opening, or query execution.</param>
    /// <returns>A task containing the mapped row or its default value.</returns>
    Task<TResult?> ExecuteAsync<TMapper, TCriteria, TResult>(QueryDescriptor<TMapper, TCriteria, TResult> descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default);
}
