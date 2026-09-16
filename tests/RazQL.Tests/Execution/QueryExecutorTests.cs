using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using RazQL.Binding;
using RazQL.Execution;
using RazQL.Template;

namespace RazQL.Tests.Execution;

public class QueryExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsGeneratedQueryAndReturnsAdapterResults()
    {
        await using var dataSource = new RecordingDataSource();
        var sqlGenerator = Substitute.For<ISqlGenerator>();
        var executionAdapter = Substitute.For<IExecutionAdapter>();
        var descriptor = CreateDescriptor();
        var criteria = new QueryCriteria { Name = "Tom" };
        var parameters = Substitute.For<IParameterBag>();
        var generatedQuery = new ParameterizedQueryResult("select id, name from artist", parameters);
        var expected = new[]
        {
            new QueryResult { Id = 1, Name = "Tom Petty" },
            new QueryResult { Id = 2, Name = "David A. Stewart" }
        };
        using var cancellation = new CancellationTokenSource();
        sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, cancellation.Token)
            .Returns(Task.FromResult(generatedQuery));
        executionAdapter.QueryAsync<QueryResult>(
                Arg.Any<DbConnection>(),
                Arg.Any<ParameterizedQueryResult>(),
                cancellation.Token)
            .Returns(expected);
        var executor = new QueryExecutor(dataSource, sqlGenerator, executionAdapter);

        var result = await executor.ExecuteAsync<QueryCriteria, QueryResult>(
            descriptor,
            criteria,
            cancellation.Token);

        Assert.Same(expected, result);
        await sqlGenerator.Received(1).ApplyCriteriaAsync(descriptor, criteria, cancellation.Token);
        await executionAdapter.Received(1).QueryAsync<QueryResult>(
            dataSource.LastConnection!,
            generatedQuery,
            cancellation.Token);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteSingleOrDefaultAsync_ReturnsAdapterResult(bool hasResult)
    {
        await using var dataSource = new RecordingDataSource();
        var sqlGenerator = Substitute.For<ISqlGenerator>();
        var executionAdapter = Substitute.For<IExecutionAdapter>();
        var descriptor = CreateDescriptor();
        var criteria = new QueryCriteria();
        var parameters = Substitute.For<IParameterBag>();
        var generatedQuery = new ParameterizedQueryResult("select id, name from artist", parameters);
        QueryResult? expected = hasResult ? new QueryResult { Id = 1, Name = "Tom Petty" } : null;
        sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, CancellationToken.None)
            .Returns(Task.FromResult(generatedQuery));
        executionAdapter.QuerySingleOrDefaultAsync<QueryResult>(
                Arg.Any<DbConnection>(),
                Arg.Any<ParameterizedQueryResult>(),
                CancellationToken.None)
            .Returns(expected);
        var executor = new QueryExecutor(dataSource, sqlGenerator, executionAdapter);

        var result = await executor.ExecuteSingleOrDefaultAsync<QueryCriteria, QueryResult>(
            descriptor,
            criteria,
            CancellationToken.None);

        Assert.Same(expected, result);
        await sqlGenerator.Received(1).ApplyCriteriaAsync(descriptor, criteria, CancellationToken.None);
        await executionAdapter.Received(1).QuerySingleOrDefaultAsync<QueryResult>(
            dataSource.LastConnection!,
            generatedQuery,
            CancellationToken.None);
    }

    private static QueryDescriptor CreateDescriptor() =>
        QueryDescriptor.ForExpression<ISqlMapper, QueryCriteria, QueryResult>(mapper => mapper.FindAsync);
}

public sealed class QueryCriteria
{
    public string? Name { get; init; }
}

public sealed record QueryResult
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
}

internal sealed class RecordingDataSource : DbDataSource
{
    public override string ConnectionString => "recording";
    public RecordingConnection? LastConnection { get; private set; }

    protected override DbConnection CreateDbConnection() =>
        LastConnection = new RecordingConnection();
}

internal sealed class RecordingConnection : DbConnection
{
    private ConnectionState _state = ConnectionState.Closed;

    [AllowNull]
    public override string ConnectionString { get; set; } = "recording";
    public override string Database => "recording";
    public override string DataSource => "recording";
    public override string ServerVersion => "1.0";
    public override ConnectionState State => _state;

    public override void ChangeDatabase(string databaseName)
    {
    }

    public override void Close() => _state = ConnectionState.Closed;

    public override void Open() => _state = ConnectionState.Open;

    public override Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Open();
        return Task.CompletedTask;
    }

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) =>
        throw new NotSupportedException();

    protected override DbCommand CreateDbCommand() =>
        throw new NotSupportedException();
}
