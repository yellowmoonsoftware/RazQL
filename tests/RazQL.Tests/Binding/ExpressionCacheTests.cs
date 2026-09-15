using System.Linq.Expressions;
using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class ExpressionCacheTests
{
    [Fact]
    public void GetMemberAndDelegate_ReturnsNestedMemberPathAndGetter()
    {
        var cache = new ExpressionCache();
        var model = new BindingModel { Address = new Address { City = "Pittsburgh" } };

        var (name, getter) = cache.GetMemberAndDelegate<BindingModel, string>(m => m.Address.City);

        Assert.Equal("Address_City", name);
        Assert.Equal("Pittsburgh", getter(model));
    }

    [Fact]
    public void GetMemberAndDelegate_UsesThisForIdentitySelector()
    {
        var cache = new ExpressionCache();

        var (name, getter) = cache.GetMemberAndDelegate<string, string>(value => value);

        Assert.Equal("this", name);
        Assert.Equal("value", getter("value"));
    }

    [Fact]
    public void GetMemberAndDelegate_RejectsComputedSelector()
    {
        var cache = new ExpressionCache();

        Assert.Throws<NotSupportedException>(() =>
            cache.GetMemberAndDelegate<BindingModel, string>(m => m.Name.ToUpperInvariant()));
    }

    [Fact]
    public void GetMemberAndDelegate_RejectsMemberNotRootedInModel()
    {
        var cache = new ExpressionCache();
        var captured = new BindingModel { Name = "captured" };

        var exception = Assert.Throws<ArgumentException>(() =>
            cache.GetMemberAndDelegate<BindingModel, string>(_ => captured.Name));

        Assert.Contains("rooted in the model parameter", exception.Message);
    }

    [Fact]
    public void UnwrapExpression_IdentifiesIdentityAndCompilesDelegate()
    {
        var cache = new ExpressionCache();
        Expression<Func<string, string>> expression = value => value;

        var (transform, isIdentity) = cache.UnwrapExpression(expression);

        Assert.True(isIdentity);
        Assert.Equal("text", transform("text"));
    }

    [Fact]
    public void UnwrapExpression_IdentifiesTransform()
    {
        var cache = new ExpressionCache();

        var (transform, isIdentity) = cache.UnwrapExpression<string, string>(value => $"%{value}%");

        Assert.False(isIdentity);
        Assert.Equal("%text%", transform("text"));
    }
}
