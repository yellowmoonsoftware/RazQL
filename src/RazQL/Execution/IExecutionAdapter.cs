using System.Data.Common;

namespace RazQL.Execution;

/// <summary>Executes generated queries through a database access provider.</summary>
public interface IExecutionAdapter
{
    /// <summary>Executes a query that returns zero or more mapped rows.</summary>
    /// <typeparam name="TResult">The row-mapping result type.</typeparam>
    /// <param name="connection">The open database connection.</param>
    /// <param name="parameterizedQueryResult">The generated SQL and its bound parameters.</param>
    /// <param name="cancellationToken">A token that cancels the database operation.</param>
    /// <returns>A task containing the mapped rows.</returns>
    Task<IEnumerable<TResult>> QueryAsync<TResult>(
        DbConnection connection,
        ParameterizedQueryResult parameterizedQueryResult,
        CancellationToken cancellationToken);

    /// <summary>Executes a query that returns zero or one mapped row.</summary>
    /// <typeparam name="TResult">The row-mapping result type.</typeparam>
    /// <param name="connection">The open database connection.</param>
    /// <param name="parameterizedQueryResult">The generated SQL and its bound parameters.</param>
    /// <param name="cancellationToken">A token that cancels the database operation.</param>
    /// <returns>A task containing the mapped row or its default value.</returns>
    Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        DbConnection connection,
        ParameterizedQueryResult parameterizedQueryResult,
        CancellationToken cancellationToken);
}
