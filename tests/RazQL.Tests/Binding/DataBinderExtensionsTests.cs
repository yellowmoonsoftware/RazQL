using System.Linq.Expressions;
using NSubstitute;
using RazQL.Binding;

namespace RazQL.Tests.Binding;

public class DataBinderExtensionsTests
{
    [Theory]
    [InlineData(NullCollectionBinding.AsNull)]
    [InlineData(NullCollectionBinding.AsEmptyArray)]
    public void BindAsArray_DelegatesWithIdentitySelectorAndNullPolicy(
        NullCollectionBinding nullCollectionBinding)
    {
        var binder = Substitute.For<IDataBinder<ICollection<long>>>();
        binder.BindAsArray(
                Arg.Any<Expression<Func<ICollection<long>, ICollection<long>?>>>(),
                nullCollectionBinding)
            .Returns("@Model");

        var result = binder.BindAsArray(nullCollectionBinding);

        Assert.Equal("@Model", result);
        binder.Received(1).BindAsArray(
            Arg.Is<Expression<Func<ICollection<long>, ICollection<long>?>>>(expression => IsIdentity(expression)),
            nullCollectionBinding);
    }

    [Fact]
    public void Bind_DelegatesWithIdentitySelector()
    {
        var binder = Substitute.For<IDataBinder<TestData>>();
        binder.Bind(Arg.Any<Expression<Func<TestData, TestData>>>()).Returns("@Model");

        var result = binder.Bind();

        Assert.Equal("@Model", result);
        binder.Received(1).Bind(
            Arg.Is<Expression<Func<TestData, TestData>>>(expression => IsIdentity(expression)));
    }

    [Fact]
    public void IsNull_DelegatesWithIdentitySelector()
    {
        var binder = Substitute.For<IDataBinder<TestData?>>();
        binder.IsNull(Arg.Any<Expression<Func<TestData?, TestData?>>>()).Returns(true);

        var result = binder.IsNull();

        Assert.True(result);
        binder.Received(1).IsNull(
            Arg.Is<Expression<Func<TestData?, TestData?>>>(expression => IsIdentity(expression)));
    }

    [Fact]
    public void IsNullOrEmpty_AcceptsNullableAndNonNullableStringModels()
    {
        var nonNullableBinder = Substitute.For<IDataBinder<string>>();
        var nullableBinder = Substitute.For<IDataBinder<string?>>();
        nonNullableBinder.IsNullOrEmpty(
                Arg.Any<Expression<Func<string, string?>>>())
            .Returns(true);
        nullableBinder.IsNullOrEmpty(
                Arg.Any<Expression<Func<string?, string?>>>())
            .Returns(false);

        var nonNullableResult = nonNullableBinder.IsNullOrEmpty();
        var nullableResult = nullableBinder.IsNullOrEmpty();

        Assert.True(nonNullableResult);
        Assert.False(nullableResult);
        nonNullableBinder.Received(1).IsNullOrEmpty(
            Arg.Is<Expression<Func<string, string?>>>(expression => IsIdentity(expression)));
        nullableBinder.Received(1).IsNullOrEmpty(
            Arg.Is<Expression<Func<string?, string?>>>(expression => IsIdentity(expression)));
    }

    [Fact]
    public void IsNullOrEmpty_AcceptsNullableAndNonNullableCollectionModels()
    {
        var nonNullableBinder = Substitute.For<IDataBinder<ICollection<long>>>();
        var nullableBinder = Substitute.For<IDataBinder<ICollection<long>?>>();
        nonNullableBinder.IsNullOrEmpty(
                Arg.Any<Expression<Func<ICollection<long>, ICollection<long>?>>>())
            .Returns(true);
        nullableBinder.IsNullOrEmpty(
                Arg.Any<Expression<Func<ICollection<long>?, ICollection<long>?>>>())
            .Returns(false);

        var nonNullableResult = nonNullableBinder.IsNullOrEmpty();
        var nullableResult = nullableBinder.IsNullOrEmpty();

        Assert.True(nonNullableResult);
        Assert.False(nullableResult);
        nonNullableBinder.Received(1).IsNullOrEmpty(
            Arg.Is<Expression<Func<ICollection<long>, ICollection<long>?>>>(expression => IsIdentity(expression)));
        nullableBinder.Received(1).IsNullOrEmpty(
            Arg.Is<Expression<Func<ICollection<long>?, ICollection<long>?>>>(expression => IsIdentity(expression)));
    }

    [Fact]
    public void Select_DelegatesWithIdentitySelector()
    {
        var binder = Substitute.For<IDataBinder<ICollection<TestData>>>();
        var selected = new[]
        {
            (Substitute.For<IDataBinder<TestData>>(), new EnumeratingContext(0, true, true, 1))
        };
        binder.Select(Arg.Any<Expression<Func<ICollection<TestData>, ICollection<TestData>>>>())
            .Returns(selected);

        var result = binder.Select();

        Assert.Same(selected, result);
        binder.Received(1).Select(
            Arg.Is<Expression<Func<ICollection<TestData>, ICollection<TestData>>>>(expression => IsIdentity(expression)));
    }

    private static bool IsIdentity(LambdaExpression expression) =>
        expression.Parameters.Count == 1 && ReferenceEquals(expression.Body, expression.Parameters[0]);

    public sealed record TestData;
}
