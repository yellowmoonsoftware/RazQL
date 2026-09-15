using System.Linq.Expressions;
using NSubstitute;
using RazQL.Binding;

namespace RazQL.Tests;

public class RazQLModelTests
{
    [Fact]
    public void Members_DelegateToConfiguredDataBinder()
    {
        var binder = Substitute.For<IDataBinder<ModelData>>();
        var childBinder = Substitute.For<IDataBinder<ModelChild>>();
        var selected = new[] { (childBinder, new EnumeratingContext(0, true, true, 1)) };
        binder.Bind(Arg.Any<Expression<Func<ModelData, string>>>()).Returns("@Name");
        binder.Bind(
                Arg.Any<Expression<Func<ModelData, string>>>(),
                Arg.Any<Expression<Func<string, string>>>())
            .Returns("@TransformedName");
        binder.BindAsArray(
                Arg.Any<Expression<Func<ModelData, ICollection<int>?>>>(),
                NullCollectionBinding.AsEmptyArray)
            .Returns("@Values");
        binder.Test(Arg.Any<Func<ModelData, bool>>()).Returns(true);
        binder.IsNull(Arg.Any<Expression<Func<ModelData, string?>>>()).Returns(true);
        binder.IsNullOrEmpty(Arg.Any<Expression<Func<ModelData, string?>>>()).Returns(false);
        binder.IsNullOrEmpty(Arg.Any<Expression<Func<ModelData, ICollection<int>?>>>()).Returns(false);
        binder.IsTrue(Arg.Any<Expression<Func<ModelData, bool?>>>()).Returns(true);
        binder.IsFalse(Arg.Any<Expression<Func<ModelData, bool?>>>()).Returns(false);
        binder.Select(Arg.Any<Expression<Func<ModelData, ICollection<ModelChild>>>>()).Returns(selected);
        binder.OrderBy(
                Arg.Any<Func<ModelData, ICollection<OrderSpec<ModelSort>>?>>(),
                Arg.Any<Func<ModelSort, string?>>())
            .Returns("ORDER BY name ASC");
        var model = new ExposedRazQLModel { Model = binder };

        Assert.Equal("@Name", model.Bind(m => m.Name));
        Assert.Equal("@TransformedName", model.Bind(m => m.Name, value => $"%{value}%"));
        Assert.Equal(
            "@Values",
            model.BindAsArray(m => m.Values, NullCollectionBinding.AsEmptyArray));
        Assert.True(model.Test(m => m.Name == "Petty"));
        Assert.True(model.IsNull(m => m.OptionalName));
        Assert.False(model.IsNullOrEmpty(m => m.Name));
        Assert.False(model.IsNullOrEmpty(m => m.Values));
        Assert.True(model.IsTrue(m => m.Enabled));
        Assert.False(model.IsFalse(m => m.Enabled));
        Assert.Same(selected, model.Select(m => m.Children));
        Assert.Equal("ORDER BY name ASC", model.OrderBy(m => m.Sort, _ => "name"));
        Assert.Equal("%Petty%", model.ApplyLike("Petty"));

        binder.Received(1).Bind(Arg.Any<Expression<Func<ModelData, string>>>());
        binder.Received(1).Bind(
            Arg.Any<Expression<Func<ModelData, string>>>(),
            Arg.Any<Expression<Func<string, string>>>());
        binder.Received(1).BindAsArray(
            Arg.Any<Expression<Func<ModelData, ICollection<int>?>>>(),
            NullCollectionBinding.AsEmptyArray);
        binder.Received(1).Test(Arg.Any<Func<ModelData, bool>>());
        binder.Received(1).IsNull(Arg.Any<Expression<Func<ModelData, string?>>>());
        binder.Received(1).IsNullOrEmpty(Arg.Any<Expression<Func<ModelData, string?>>>());
        binder.Received(1).IsNullOrEmpty(Arg.Any<Expression<Func<ModelData, ICollection<int>?>>>());
        binder.Received(1).IsTrue(Arg.Any<Expression<Func<ModelData, bool?>>>());
        binder.Received(1).IsFalse(Arg.Any<Expression<Func<ModelData, bool?>>>());
        binder.Received(1).Select(Arg.Any<Expression<Func<ModelData, ICollection<ModelChild>>>>());
        binder.Received(1).OrderBy(
            Arg.Any<Func<ModelData, ICollection<OrderSpec<ModelSort>>?>>(),
            Arg.Any<Func<ModelSort, string?>>());
    }
}

internal sealed class ExposedRazQLModel : RazQLModel<ModelData>
{
    public string ApplyLike(string value) => Like.Compile()(value);
}

public sealed class ModelData
{
    public string Name { get; init; } = "";
    public string? OptionalName { get; init; }
    public ICollection<int> Values { get; init; } = [];
    public bool? Enabled { get; init; }
    public ICollection<ModelChild> Children { get; init; } = [];
    public ICollection<OrderSpec<ModelSort>> Sort { get; init; } = [];
}

public sealed class ModelChild;

public enum ModelSort
{
    Name
}
