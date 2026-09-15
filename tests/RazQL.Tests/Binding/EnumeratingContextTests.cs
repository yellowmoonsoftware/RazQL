using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class EnumeratingContextTests
{
    [Theory]
    [InlineData(false, ",", ",")]
    [InlineData(false, " AND ", " AND ")]
    [InlineData(true, ",", "")]
    public void Separator_EmitsOnlyBetweenItems(bool isLast, string separator, string expected)
    {
        var context = new EnumeratingContext(0, true, isLast, isLast ? 1 : 2);

        Assert.Equal(expected, context.Separator(separator));
    }
}
