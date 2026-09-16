using NSubstitute;
using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class DefaultDataBinderContextFactoryTests
{
    [Fact]
    public void Create_UsesFreshNamingScopeAndParametersForEachQuery()
    {
        var providerFactory = Substitute.For<IParameterNameProviderFactory>();
        var firstProvider = Substitute.For<IParameterNameProvider>();
        var secondProvider = Substitute.For<IParameterNameProvider>();
        providerFactory.Create().Returns(firstProvider, secondProvider);
        firstProvider.GetStableName("Name").Returns("first_name");
        secondProvider.GetStableName("Name").Returns("second_name");
        var factory = new DefaultDataBinderContextFactory(providerFactory, new DataBinderOptions());

        var first = factory.Create(new NameCriteria("first"));
        var second = factory.Create(new NameCriteria("second"));
        var firstBinding = first.DataBinder.Bind(criteria => criteria.Name);
        var secondBinding = second.DataBinder.Bind(criteria => criteria.Name);

        Assert.Equal("@first_name", firstBinding);
        Assert.Equal("@second_name", secondBinding);
        Assert.Equal("first", first.Parameters.Get<string>("first_name"));
        Assert.Equal("second", second.Parameters.Get<string>("second_name"));
        Assert.NotSame(first.Parameters, second.Parameters);
        providerFactory.Received(2).Create();
    }

    [Fact]
    public void Create_PassesConfiguredOptionsToBinder()
    {
        var providerFactory = Substitute.For<IParameterNameProviderFactory>();
        providerFactory.Create().Returns(Substitute.For<IParameterNameProvider>());
        var options = new DataBinderOptions(
            new Dictionary<OrderByDirection, string> { [OrderByDirection.Asc] = "UP" },
            DataBinderOptions.DefaultOrderByNullsClause);
        var factory = new DefaultDataBinderContextFactory(providerFactory, options);
        var criteria = new SortCriteria([new OrderSpec<SortColumn>(SortColumn.Name, OrderByDirection.Asc)]);

        var context = factory.Create(criteria);
        var sql = context.DataBinder.OrderBy(value => value.Sorting, _ => "name");

        Assert.Equal("ORDER BY name UP", sql);
    }

    private sealed record NameCriteria(string Name);
    private sealed record SortCriteria(ICollection<OrderSpec<SortColumn>> Sorting);
    private enum SortColumn { Name }
}
