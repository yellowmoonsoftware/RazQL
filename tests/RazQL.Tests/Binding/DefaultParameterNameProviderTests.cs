using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class DefaultParameterNameProviderTests
{
    [Fact]
    public void GetStableName_ReusesRegisteredName()
    {
        var provider = new DefaultParameterNameProvider();

        Assert.Equal("Name", provider.GetStableName("Name"));
        Assert.Equal("Name", provider.GetStableName("Name"));
    }

    [Fact]
    public void GetUniqueName_GeneratesHexadecimalSuffixes()
    {
        var provider = new DefaultParameterNameProvider();

        var names = Enumerable.Range(0, 10)
            .Select(_ => provider.GetUniqueName("Name"))
            .ToArray();

        Assert.Equal("Name_01", names[0]);
        Assert.Equal("Name_0a", names[9]);
    }

    [Fact]
    public void GetStableName_AvoidsPreviouslyGeneratedNameAndReusesItsCollisionName()
    {
        var provider = new DefaultParameterNameProvider();
        Assert.Equal("Name_01", provider.GetUniqueName("Name"));

        var first = provider.GetStableName("Name_01");
        var second = provider.GetStableName("Name_01");

        Assert.Equal("Name_01_01", first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void WithPrefix_ComposesNestedEnumerationPath()
    {
        var provider = new DefaultParameterNameProvider()
            .WithPrefix("Orders", new IParameterNameProvider.EnumeratingContext(2))
            .WithPrefix("Lines", new IParameterNameProvider.EnumeratingContext(1));

        Assert.Equal("Orders_2_Lines_1_ProductId", provider.GetStableName("ProductId"));
    }

    [Fact]
    public void WithPrefix_SharesNameRegistryWithParent()
    {
        var provider = new DefaultParameterNameProvider();
        var child = provider.WithPrefix("Scope", null);

        Assert.Equal("Scope_Name_01", child.GetUniqueName("Name"));
        Assert.Equal("Scope_Name_01_01", provider.GetStableName("Scope_Name_01"));
    }

    [Fact]
    public void WithPrefix_RejectsWhitespace()
    {
        var provider = new DefaultParameterNameProvider();

        Assert.Throws<ArgumentException>(() => provider.WithPrefix(" ", null));
    }
}
