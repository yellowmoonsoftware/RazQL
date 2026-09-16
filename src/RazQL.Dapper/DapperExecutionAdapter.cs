using System.Data.Common;
using Dapper;
using RazQL.Execution;

namespace RazQL.Dapper;

/// <summary>Executes RazQL commands through Dapper.</summary>
public sealed class DapperExecutionAdapter : IExecutionAdapter
{
    /// <inheritdoc />
    public Task<IEnumerable<TResult>> QueryAsync<TResult>(
        DbConnection connection,
        ParameterizedQueryResult parameterizedQueryResult,
        CancellationToken cancellationToken) =>
        connection.QueryAsync<TResult>(parameterizedQueryResult.ToCommandDefinition(cancellationToken));

    /// <inheritdoc />
    public Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        DbConnection connection,
        ParameterizedQueryResult parameterizedQueryResult,
        CancellationToken cancellationToken) =>
        connection.QuerySingleOrDefaultAsync<TResult>(parameterizedQueryResult.ToCommandDefinition(cancellationToken));
}
