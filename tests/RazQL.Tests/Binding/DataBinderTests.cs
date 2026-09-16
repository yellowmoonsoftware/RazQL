using System.Linq.Expressions;
using NSubstitute;
using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class DataBinderTests
{
    [Fact]
    public void Bind_AddsNamedParameterAndReusesIt()
    {
        var parameters = new RecordingParameterBag();
        var nameProvider = Substitute.For<IParameterNameProvider>();
        nameProvider.GetStableName("Name").Returns("artist_name");
        var expressionCache = Substitute.For<IExpressionCache>();
        var getterCalls = 0;
        expressionCache.GetMemberAndDelegate(Arg.Any<Expression<Func<BindingModel, string>>>())
            .Returns(("Name", model =>
            {
                getterCalls++;
                return model.Name;
            }));
        var binder = CreateBinder(new BindingModel { Name = "Tom Petty" }, parameters, nameProvider, expressionCache);

        var first = binder.Bind(m => m.Name);
        var second = binder.Bind(m => m.Name);

        Assert.Equal("@artist_name", first);
        Assert.Equal(first, second);
        Assert.Equal("Tom Petty", parameters.Get<string>("artist_name"));
        Assert.Single(parameters.ParameterNames);
        Assert.Equal(1, getterCalls);
        nameProvider.Received(2).GetStableName("Name");
    }

    [Fact]
    public void Bind_UsesNestedMemberPath()
    {
        var parameters = new RecordingParameterBag();
        var nameProvider = Substitute.For<IParameterNameProvider>();
        nameProvider.GetStableName("Address_City").Returns("city");
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(Arg.Any<Expression<Func<BindingModel, string>>>())
            .Returns(("Address_City", model => model.Address.City));
        var binder = CreateBinder(
            new BindingModel { Address = new Address { City = "Pittsburgh" } },
            parameters,
            nameProvider,
            expressionCache);

        Assert.Equal("@city", binder.Bind(m => m.Address.City));
        Assert.Equal("Pittsburgh", parameters.Get<string>("city"));
    }

    [Fact]
    public void BindTransform_GeneratesNewParameterForEveryEvaluation()
    {
        var parameters = new RecordingParameterBag();
        var nameProvider = Substitute.For<IParameterNameProvider>();
        nameProvider.GetUniqueName("Namex").Returns("like_01", "like_02");
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(Arg.Any<Expression<Func<BindingModel, string>>>())
            .Returns(("Name", model => model.Name));
        expressionCache.UnwrapExpression(Arg.Any<Expression<Func<string, string>>>())
            .Returns((value => $"%{value}%", false));
        var binder = CreateBinder(new BindingModel { Name = "Petty" }, parameters, nameProvider, expressionCache);

        var first = binder.Bind(m => m.Name, value => $"%{value}%");
        var second = binder.Bind(m => m.Name, value => $"%{value}%");

        Assert.Equal("@like_01", first);
        Assert.Equal("@like_02", second);
        Assert.Equal("%Petty%", parameters.Get<string>("like_01"));
        Assert.Equal("%Petty%", parameters.Get<string>("like_02"));
    }

    [Fact]
    public void BindAsArray_MaterializesCollectionAndReusesStableParameter()
    {
        var parameters = new RecordingParameterBag();
        var nameProvider = Substitute.For<IParameterNameProvider>();
        nameProvider.GetStableName("Values").Returns("ids");
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(
                Arg.Any<Expression<Func<BindingModel, ICollection<int>>>>())
            .Returns(("Values", model => model.Values));
        var binder = CreateBinder(
            new BindingModel { Values = new List<int> { 3, 5, 8 } },
            parameters,
            nameProvider,
            expressionCache);

        var first = binder.BindAsArray(m => m.Values);
        var second = binder.BindAsArray(m => m.Values);

        Assert.Equal("@ids", first);
        Assert.Equal(first, second);
        Assert.Equal([3, 5, 8], parameters.Get<int[]>("ids"));
        Assert.Single(parameters.ParameterNames);
    }

    [Fact]
    public void BindAsArray_PreservesNullCollectionByDefault()
    {
        var parameters = new RecordingParameterBag();
        var nameProvider = Substitute.For<IParameterNameProvider>();
        nameProvider.GetStableName("OptionalValues").Returns("ids");
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(
                Arg.Any<Expression<Func<BindingModel, ICollection<int>?>>>())
            .Returns(("OptionalValues", model => model.OptionalValues));
        var binder = CreateBinder(
            new BindingModel { OptionalValues = null },
            parameters,
            nameProvider,
            expressionCache);

        var parameterName = binder.BindAsArray(m => m.OptionalValues);

        Assert.Equal("@ids", parameterName);
        Assert.Null(parameters.Get<int[]?>("ids"));
    }

    [Fact]
    public void BindAsArray_EmptyArrayPolicyUsesDistinctReusableParameter()
    {
        var parameters = new RecordingParameterBag();
        var nameProvider = Substitute.For<IParameterNameProvider>();
        nameProvider.GetStableName("OptionalValues").Returns("ids");
        nameProvider.GetStableName("OptionalValues_orempty").Returns("ids_orempty");
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(
                Arg.Any<Expression<Func<BindingModel, ICollection<int>?>>>())
            .Returns(("OptionalValues", model => model.OptionalValues));
        var binder = CreateBinder(
            new BindingModel { OptionalValues = null },
            parameters,
            nameProvider,
            expressionCache);

        var nullableParameter = binder.BindAsArray(m => m.OptionalValues);
        var emptyParameter = binder.BindAsArray(
            m => m.OptionalValues,
            NullCollectionBinding.AsEmptyArray);
        var repeatedEmptyParameter = binder.BindAsArray(
            m => m.OptionalValues,
            NullCollectionBinding.AsEmptyArray);

        Assert.Equal("@ids", nullableParameter);
        Assert.Equal("@ids_orempty", emptyParameter);
        Assert.Equal(emptyParameter, repeatedEmptyParameter);
        Assert.Null(parameters.Get<int[]?>("ids"));
        Assert.Empty(parameters.Get<int[]>("ids_orempty"));
        Assert.Equal(2, parameters.ParameterNames.Count());
    }

    [Fact]
    public void BindAsArray_UnknownPolicyUsesDefaultNullBehavior()
    {
        var parameters = new RecordingParameterBag();
        var nameProvider = Substitute.For<IParameterNameProvider>();
        nameProvider.GetStableName("OptionalValues").Returns("ids");
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(
                Arg.Any<Expression<Func<BindingModel, ICollection<int>?>>>())
            .Returns(("OptionalValues", model => model.OptionalValues));
        var binder = CreateBinder(
            new BindingModel { OptionalValues = null },
            parameters,
            nameProvider,
            expressionCache);

        var parameterName = binder.BindAsArray(
            m => m.OptionalValues,
            (NullCollectionBinding)100);

        Assert.Equal("@ids", parameterName);
        Assert.Null(parameters.Get<int[]?>("ids"));
    }

    [Fact]
    public void Predicates_EvaluateModelValues()
    {
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(Arg.Any<Expression<Func<BindingModel, string?>>>())
            .Returns(("OptionalName", model => model.OptionalName), ("Name", model => model.Name));
        expressionCache.GetMemberAndDelegate(
                Arg.Any<Expression<Func<BindingModel, ICollection<int>?>>>())
            .Returns(("OptionalValues", model => model.OptionalValues));
        expressionCache.GetMemberAndDelegate(Arg.Any<Expression<Func<BindingModel, bool?>>>())
            .Returns(("Enabled", model => model.Enabled));
        var binder = CreateBinder(new BindingModel
        {
            Name = "",
            OptionalName = null,
            OptionalValues = [],
            Enabled = null
        }, expressionCache: expressionCache);

        Assert.True(binder.Test(m => m.Name.Length == 0));
        Assert.True(binder.IsNull(m => m.OptionalName));
        Assert.True(binder.IsNullOrEmpty(m => m.Name));
        Assert.True(binder.IsNullOrEmpty(m => m.OptionalValues));
        Assert.False(binder.IsTrue(m => m.Enabled));
        Assert.True(binder.IsFalse(m => m.Enabled));
    }

    [Fact]
    public void Select_CreatesIndexedChildBindersAndContexts()
    {
        var parameters = new RecordingParameterBag();
        var model = new BindingModel
        {
            Children =
            [
                new ChildModel { Name = "first" },
                new ChildModel { Name = "second" }
            ]
        };
        var nameProvider = Substitute.For<IParameterNameProvider>();
        var firstChildNameProvider = Substitute.For<IParameterNameProvider>();
        var secondChildNameProvider = Substitute.For<IParameterNameProvider>();
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(
                Arg.Any<Expression<Func<BindingModel, ICollection<ChildModel>>>>())
            .Returns(("Children", value => value.Children));
        expressionCache.GetMemberAndDelegate(Arg.Any<Expression<Func<ChildModel, string>>>())
            .Returns(("Name", value => value.Name));
        nameProvider.WithPrefix("Children", new IParameterNameProvider.EnumeratingContext(0))
            .Returns(firstChildNameProvider);
        nameProvider.WithPrefix("Children", new IParameterNameProvider.EnumeratingContext(1))
            .Returns(secondChildNameProvider);
        firstChildNameProvider.GetStableName("Name").Returns("first_child_name");
        secondChildNameProvider.GetStableName("Name").Returns("second_child_name");
        var binder = CreateBinder(model, parameters, nameProvider, expressionCache);

        var selected = binder.Select(m => m.Children).ToArray();
        var firstName = selected[0].Item1.Bind(m => m.Name);
        var secondName = selected[1].Item1.Bind(m => m.Name);

        Assert.Equal("@first_child_name", firstName);
        Assert.Equal("@second_child_name", secondName);
        Assert.Equal("first", parameters.Get<string>("first_child_name"));
        Assert.Equal("second", parameters.Get<string>("second_child_name"));
        Assert.Equal(new EnumeratingContext(0, true, false, 2), selected[0].Item2);
        Assert.Equal(new EnumeratingContext(1, false, true, 2), selected[1].Item2);
    }

    [Fact]
    public void Select_PreservesCustomOptionsInChildBinder()
    {
        var options = new DataBinderOptions(
            new Dictionary<OrderByDirection, string>
            {
                [OrderByDirection.Asc] = "UP",
                [OrderByDirection.Desc] = "DOWN"
            },
            DataBinderOptions.DefaultOrderByNullsClause);
        var nameProvider = Substitute.For<IParameterNameProvider>();
        var expressionCache = Substitute.For<IExpressionCache>();
        expressionCache.GetMemberAndDelegate(
                Arg.Any<Expression<Func<BindingModel, ICollection<ChildModel>>>>())
            .Returns(("Children", value => value.Children));
        nameProvider.WithPrefix("Children", new IParameterNameProvider.EnumeratingContext(0))
            .Returns(Substitute.For<IParameterNameProvider>());
        var binder = CreateBinder(new BindingModel
        {
            Children =
            [
                new ChildModel
                {
                    Sort = [new OrderSpec<SortColumn>(SortColumn.Name, OrderByDirection.Desc)]
                }
            ]
        }, nameProvider: nameProvider, expressionCache: expressionCache, options: options);

        var child = binder.Select(m => m.Children).Single().Item1;

        Assert.Equal("ORDER BY name DOWN", child.OrderBy(m => m.Sort, column => column == SortColumn.Name ? "name" : null));
    }

    [Fact]
    public void OrderBy_EmitsConfiguredClausesAndSkipsUnknownColumns()
    {
        var binder = CreateBinder(new BindingModel
        {
            Sort =
            [
                new OrderSpec<SortColumn>(SortColumn.Name, OrderByDirection.Asc, OrderByNulls.Last),
                new OrderSpec<SortColumn>(SortColumn.Unsupported, OrderByDirection.Desc),
                new OrderSpec<SortColumn>(SortColumn.Name, OrderByDirection.Desc, OrderByNulls.First)
            ]
        });

        var sql = binder.OrderBy(m => m.Sort, column => column == SortColumn.Name ? "artist_name" : null);

        Assert.Equal("ORDER BY artist_name ASC NULLS LAST,artist_name DESC NULLS FIRST", sql);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrderBy_ReturnsEmptyWhenNoUsableCriteria(bool useNull)
    {
        var model = new BindingModel
        {
            Sort = useNull ? null : [new OrderSpec<SortColumn>(SortColumn.Unsupported)]
        };
        var binder = CreateBinder(model);

        Assert.Equal("", binder.OrderBy(m => m.Sort, _ => null));
    }

    private static DataBinder<BindingModel> CreateBinder(
        BindingModel model,
        RecordingParameterBag? parameters = null,
        IParameterNameProvider? nameProvider = null,
        IExpressionCache? expressionCache = null,
        DataBinderOptions? options = null) =>
        new(model, parameters ?? new RecordingParameterBag(),
            nameProvider ?? Substitute.For<IParameterNameProvider>(),
            options ?? new DataBinderOptions(),
            expressionCache ?? Substitute.For<IExpressionCache>());
}

public sealed class BindingModel
{
    public string Name { get; init; } = "";
    public string? OptionalName { get; init; }
    public Address Address { get; init; } = new();
    public ICollection<int> Values { get; init; } = [];
    public ICollection<int>? OptionalValues { get; init; }
    public bool? Enabled { get; init; }
    public ICollection<ChildModel> Children { get; init; } = [];
    public ICollection<OrderSpec<SortColumn>>? Sort { get; init; }
}

public sealed class Address
{
    public string City { get; init; } = "";
}

public sealed class ChildModel
{
    public string Name { get; init; } = "";
    public ICollection<OrderSpec<SortColumn>>? Sort { get; init; }
}

public enum SortColumn
{
    Name,
    Unsupported
}
