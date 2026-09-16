using Dapper;
using NSubstitute;
using RazQL.Binding;
using RazQL.Dapper;
using RazQL.Execution;

namespace RazQL.Dapper.Tests;

public class DapperGeneratedSqlExtensionsTests
{
    [Fact]
    public void ToDynamicParameters_CopiesValuesFromBag()
    {
        var bag = Substitute.For<IParameterBag>();
        long[] ids = [1, 2];
        bag.GetParameters().Returns(
        [
            new KeyValuePair<string, object?>("name", null),
            new KeyValuePair<string, object?>("ids", ids)
        ]);

        var result = bag.ToDynamicParameters();

        Assert.Null(result.Get<object?>("name"));
        Assert.Same(ids, result.Get<long[]>("ids"));
        bag.Received(1).GetParameters();
    }

    [Fact]
    public void ToCommandDefinition_CopiesSqlParametersAndCancellationToken()
    {
        var bag = Substitute.For<IParameterBag>();
        bag.GetParameters().Returns([new KeyValuePair<string, object?>("id", 42L)]);
        var query = new ParameterizedQueryResult("select @id", bag);
        using var cancellation = new CancellationTokenSource();

        var command = query.ToCommandDefinition(cancellation.Token);

        Assert.Equal("select @id", command.CommandText);
        Assert.Equal(cancellation.Token, command.CancellationToken);
        Assert.Equal(42L, Assert.IsType<DynamicParameters>(command.Parameters).Get<long>("id"));
    }

    [Fact]
    public void Conversion_RejectsNullInputs()
    {
        Assert.Throws<ArgumentNullException>(() => DapperGeneratedSqlExtensions.ToDynamicParameters(null!));
        Assert.Throws<ArgumentNullException>(() => DapperGeneratedSqlExtensions.ToCommandDefinition(null!));
    }
}
