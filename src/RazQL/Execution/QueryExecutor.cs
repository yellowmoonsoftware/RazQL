using System.Data.Common;
using RazQL.Template;

namespace RazQL.Execution;

/// <summary>Opens connections from a data source and delegates query execution to an execution adapter.</summary>
/// <param name="dataSource">The provider-specific database data source.</param>
/// <param name="sqlGenerator">The SQL generator.</param>
/// <param name="executorAdapter">The adapter that hands off execution to a specific provider.</param>
public sealed class QueryExecutor(
    DbDataSource dataSource,
    ISqlGenerator sqlGenerator,
    IExecutionAdapter executorAdapter) : IQueryExecutor
{
    /// <inheritdoc />
    public async Task<IEnumerable<TResult>> ExecuteAsync<TCriteria, TResult>(QueryDescriptor descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var generatedQuery = await sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, cancellationToken);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await executorAdapter.QueryAsync<TResult>(connection, generatedQuery, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TResult?> ExecuteSingleOrDefaultAsync<TCriteria, TResult>(QueryDescriptor descriptor, TCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var generatedQuery = await sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, cancellationToken);
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        return await executorAdapter.QuerySingleOrDefaultAsync<TResult>(connection, generatedQuery, cancellationToken);
    }
}
