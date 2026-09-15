using System.Data.Common;
using Dapper;

namespace RazQL.Execution;

/// <summary>Executes RazQL commands through Dapper.</summary>
public sealed class DapperExecutor : IDapperExecutor
{
    /// <inheritdoc />
    public Task<IEnumerable<TResult>> QueryAsync<TResult>(
        DbConnection connection,
        CommandDefinition commandDefinition) =>
        connection.QueryAsync<TResult>(commandDefinition);

    /// <inheritdoc />
    public Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        DbConnection connection,
        CommandDefinition commandDefinition) =>
        connection.QuerySingleOrDefaultAsync<TResult>(commandDefinition);
}
