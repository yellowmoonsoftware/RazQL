using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Generates and executes SQL described by a mapper query.</summary>
public interface IQueryExecutor
{
    /// <summary>Executes a query that returns zero or more mapped rows.</summary>
    /// <typeparam name="TCriteria">The query criteria type.</typeparam>
    /// <typeparam name="TResult">The row-mapping result type.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="criteria">The criteria passed to the Razor template.</param>
    /// <param name="cancellationToken">A token that cancels SQL generation, connection opening, or query execution.</param>
    /// <returns>A task containing the mapped rows.</returns>
    Task<IEnumerable<TResult>> ExecuteAsync<TCriteria, TResult>(QueryDescriptor descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>Executes a query that returns zero or one mapped row.</summary>
    /// <typeparam name="TCriteria">The query criteria type.</typeparam>
    /// <typeparam name="TResult">The row-mapping result type.</typeparam>
    /// <param name="descriptor">The mapper query descriptor.</param>
    /// <param name="criteria">The criteria passed to the Razor template.</param>
    /// <param name="cancellationToken">A token that cancels SQL generation, connection opening, or query execution.</param>
    /// <returns>A task containing the mapped row or its default value.</returns>
    Task<TResult?> ExecuteSingleOrDefaultAsync<TCriteria, TResult>(QueryDescriptor descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default);
}
