using System.Data.Common;
using Microsoft.Extensions.Logging;
using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Opens connections from a data source and delegates query execution to an execution adapter.</summary>
/// <param name="dataSource">The provider-specific database data source.</param>
/// <param name="sqlGenerator">The SQL generator.</param>
/// <param name="executorAdapter">The adapter that hands off execution to a specific provider.</param>
/// <param name="logger">The logger used for query-execution diagnostics.</param>
public sealed partial class QueryExecutor(
    DbDataSource dataSource,
    ISqlGenerator sqlGenerator,
    IExecutionAdapter executorAdapter,
    ILogger<QueryExecutor> logger) : IQueryExecutor
{
    /// <inheritdoc />
    public async Task<IEnumerable<TResult>> ExecuteAsync<TMapper, TCriteria, TResult>(QueryDescriptor<TMapper, TCriteria, IEnumerable<TResult>> descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var generatedQuery = await sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, cancellationToken);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        LogExecution(descriptor);
        return await executorAdapter.QueryAsync<TResult>(connection, generatedQuery, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult?> ExecuteAsync<TMapper, TCriteria, TResult>(QueryDescriptor<TMapper, TCriteria, TResult> descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var generatedQuery = await sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        LogExecution(descriptor);
        return await executorAdapter.QuerySingleOrDefaultAsync<TResult>(connection, generatedQuery, cancellationToken);
    }

    [LoggerMessage(LogLevel.Information, "Executing query: {descriptor}")]
    private partial void LogExecution(QueryDescriptor descriptor);
}
