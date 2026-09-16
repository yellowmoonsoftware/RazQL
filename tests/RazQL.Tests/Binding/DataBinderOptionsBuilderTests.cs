using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class DataBinderOptionsBuilderTests
{
    [Fact]
    public void Build_PreservesEarlierSnapshotWhenBuilderChanges()
    {
        var builder = new DataBinderOptionsBuilder();
        builder.WithOrderByDirectionClause(OrderByDirection.Asc, "UP");
        var first = builder.Build();

        builder.WithOrderByDirectionClause(OrderByDirection.Asc, "HIGHER");
        var second = builder.Build();

        Assert.Equal("UP", first.OrderByDirectionClause[OrderByDirection.Asc]);
        Assert.Equal("HIGHER", second.OrderByDirectionClause[OrderByDirection.Asc]);
        Assert.Equal("DESC", second.OrderByDirectionClause[OrderByDirection.Desc]);
    }

    [Fact]
    public void WithOptions_ReplacesMappingsAndAllowsLaterOverrides()
    {
        var options = new DataBinderOptions(
            new Dictionary<OrderByDirection, string> { [OrderByDirection.Desc] = "DOWN" },
            new Dictionary<OrderByNulls, string> { [OrderByNulls.Last] = "AT END" });

        var builder = new DataBinderOptionsBuilder();
        builder.WithOptions(options).WithOrderByDirectionClause(OrderByDirection.Asc, "UP");
        var result = builder.Build();

        Assert.Equal("UP", result.OrderByDirectionClause[OrderByDirection.Asc]);
        Assert.Equal("DOWN", result.OrderByDirectionClause[OrderByDirection.Desc]);
        Assert.False(result.OrderByNullsClause.ContainsKey(OrderByNulls.First));
        Assert.Equal("AT END", result.OrderByNullsClause[OrderByNulls.Last]);
    }
}
