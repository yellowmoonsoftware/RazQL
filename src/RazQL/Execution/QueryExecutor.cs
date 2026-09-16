using System.Data.Common;
using Dapper;
using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Default query executor that opens connections from a data source and delegates mapping to Dapper.</summary>
/// <param name="dataSource">The provider-specific database data source.</param>
/// <param name="sqlGenerator">The SQL generator.</param>
/// <param name="dapperExecutor">The Dapper operation adapter.</param>
public sealed class QueryExecutor(
    DbDataSource dataSource,
    ISqlGenerator sqlGenerator,
    IDapperExecutor dapperExecutor) : IQueryExecutor
{
    /// <inheritdoc />
    public async Task<IEnumerable<TResult>> ExecuteAsync<TCriteria, TResult>(QueryDescriptor descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var (sql, dbParams) = await sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, cancellationToken);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var commandDefinition = new CommandDefinition(
            commandText: sql,
            parameters: dbParams,
            cancellationToken: cancellationToken);
        return await dapperExecutor.QueryAsync<TResult>(connection, commandDefinition);
    }

    /// <inheritdoc />
    public async Task<TResult?> ExecuteSingleOrDefaultAsync<TCriteria, TResult>(QueryDescriptor descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var (sql, dbParams) = await sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var commandDefinition = new CommandDefinition(
            commandText: sql,
            parameters: dbParams,
            cancellationToken: cancellationToken);
        return await dapperExecutor.QuerySingleOrDefaultAsync<TResult>(connection, commandDefinition);
    }
}
