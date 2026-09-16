using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class DefaultParameterNameProviderFactoryTests
{
    [Fact]
    public void Create_ReturnsIndependentNamingScopes()
    {
        var factory = new DefaultParameterNameProviderFactory();

        var first = factory.Create();
        var second = factory.Create();

        Assert.NotSame(first, second);
        Assert.Equal("Name_01", first.GetUniqueName("Name"));
        Assert.Equal("Name_01", second.GetUniqueName("Name"));
    }
}
