using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class DataBinderOptionsTests
{
    [Fact]
    public void Constructor_CopiesInputMappings()
    {
        var directions = new Dictionary<OrderByDirection, string>
        {
            [OrderByDirection.Asc] = "UP"
        };
        var nulls = new Dictionary<OrderByNulls, string>
        {
            [OrderByNulls.Last] = "NULLS END"
        };

        var options = new DataBinderOptions(directions, nulls);
        directions[OrderByDirection.Asc] = "MUTATED";
        nulls[OrderByNulls.Last] = "MUTATED";

        Assert.Equal("UP", options.OrderByDirectionClause[OrderByDirection.Asc]);
        Assert.Equal("NULLS END", options.OrderByNullsClause[OrderByNulls.Last]);
    }

    [Fact]
    public void InitProperties_CopyInputMappings()
    {
        var directions = new Dictionary<OrderByDirection, string>
        {
            [OrderByDirection.Asc] = "UP"
        };
        var options = new DataBinderOptions { OrderByDirectionClause = directions };
        directions[OrderByDirection.Asc] = "MUTATED";

        Assert.Equal("UP", options.OrderByDirectionClause[OrderByDirection.Asc]);
    }

    [Fact]
    public void DefaultConstructor_UsesStandardMappings()
    {
        var options = new DataBinderOptions();

        Assert.Equal("ASC", options.OrderByDirectionClause[OrderByDirection.Asc]);
        Assert.Equal("DESC", options.OrderByDirectionClause[OrderByDirection.Desc]);
        Assert.Equal("NULLS FIRST", options.OrderByNullsClause[OrderByNulls.First]);
        Assert.Equal("NULLS LAST", options.OrderByNullsClause[OrderByNulls.Last]);
    }
}
