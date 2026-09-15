using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Dapper;
using NSubstitute;
using RazQL.Execution;
using RazQL.Template;

namespace RazQL.Tests.Execution;

public class QueryExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_UsesGeneratedCommandAndReturnsDapperResults()
    {
        await using var dataSource = new RecordingDataSource();
        var sqlGenerator = Substitute.For<ISqlGenerator>();
        var dapperExecutor = Substitute.For<IDapperExecutor>();
        var descriptor = CreateDescriptor();
        var criteria = new QueryCriteria { Name = "Tom" };
        var parameters = new DynamicParameters();
        var expected = new[]
        {
            new QueryResult { Id = 1, Name = "Tom Petty" },
            new QueryResult { Id = 2, Name = "David A. Stewart" }
        };
        using var cancellation = new CancellationTokenSource();
        sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, cancellation.Token)
            .Returns(Task.FromResult(("select id, name from artist", parameters)));
        dapperExecutor.QueryAsync<QueryResult>(
                Arg.Any<DbConnection>(),
                Arg.Any<CommandDefinition>())
            .Returns(expected);
        var executor = new QueryExecutor(dataSource, sqlGenerator, dapperExecutor);

        var result = await executor.ExecuteAsync<QueryCriteria, QueryResult>(
            descriptor,
            criteria,
            cancellation.Token);

        Assert.Same(expected, result);
        await sqlGenerator.Received(1).ApplyCriteriaAsync(descriptor, criteria, cancellation.Token);
        await dapperExecutor.Received(1).QueryAsync<QueryResult>(
            dataSource.LastConnection!,
            Arg.Is<CommandDefinition>(command =>
                command.CommandText == "select id, name from artist" &&
                ReferenceEquals(command.Parameters, parameters) &&
                command.CancellationToken == cancellation.Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteSingleOrDefaultAsync_ReturnsDapperResult(bool hasResult)
    {
        await using var dataSource = new RecordingDataSource();
        var sqlGenerator = Substitute.For<ISqlGenerator>();
        var dapperExecutor = Substitute.For<IDapperExecutor>();
        var descriptor = CreateDescriptor();
        var criteria = new QueryCriteria();
        var parameters = new DynamicParameters();
        QueryResult? expected = hasResult ? new QueryResult { Id = 1, Name = "Tom Petty" } : null;
        sqlGenerator.ApplyCriteriaAsync(descriptor, criteria, CancellationToken.None)
            .Returns(Task.FromResult(("select id, name from artist", parameters)));
        dapperExecutor.QuerySingleOrDefaultAsync<QueryResult>(
                Arg.Any<DbConnection>(),
                Arg.Any<CommandDefinition>())
            .Returns(expected);
        var executor = new QueryExecutor(dataSource, sqlGenerator, dapperExecutor);

        var result = await executor.ExecuteSingleOrDefaultAsync<QueryCriteria, QueryResult>(
            descriptor,
            criteria,
            CancellationToken.None);

        Assert.Same(expected, result);
        await sqlGenerator.Received(1).ApplyCriteriaAsync(descriptor, criteria, CancellationToken.None);
        await dapperExecutor.Received(1).QuerySingleOrDefaultAsync<QueryResult>(
            dataSource.LastConnection!,
            Arg.Is<CommandDefinition>(command =>
                command.CommandText == "select id, name from artist" &&
                ReferenceEquals(command.Parameters, parameters) &&
                command.CancellationToken == CancellationToken.None));
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
