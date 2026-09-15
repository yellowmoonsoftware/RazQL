using System.Data.Common;
using Dapper;

namespace RazQL.Execution;

/// <summary>Abstracts the Dapper operations used to execute generated queries.</summary>
public interface IDapperExecutor
{
    /// <summary>Executes a query that returns zero or more mapped rows.</summary>
    /// <typeparam name="TResult">The row-mapping result type.</typeparam>
    /// <param name="connection">The open database connection.</param>
    /// <param name="commandDefinition">The Dapper command to execute.</param>
    /// <returns>A task containing the mapped rows.</returns>
    Task<IEnumerable<TResult>> QueryAsync<TResult>(
        DbConnection connection,
        CommandDefinition commandDefinition);

    /// <summary>Executes a query that returns zero or one mapped row.</summary>
    /// <typeparam name="TResult">The row-mapping result type.</typeparam>
    /// <param name="connection">The open database connection.</param>
    /// <param name="commandDefinition">The Dapper command to execute.</param>
    /// <returns>A task containing the mapped row or its default value.</returns>
    Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        DbConnection connection,
        CommandDefinition commandDefinition);
}
