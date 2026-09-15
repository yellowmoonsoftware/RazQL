using System.Collections;
using RazQL.Internal;

namespace RazQL.Tests.Internal;

public class ExpressionAndTypeExtensionsTests
{
    [Fact]
    public void IsOrImplements_GenericOverloadRecognizesImplementedInterface()
    {
        Assert.True(typeof(List<int>).IsOrImplements<IEnumerable>());
    }

    [Fact]
    public void IsOrImplements_RecognizesExactNonGenericInterface()
    {
        Assert.True(typeof(IEnumerable).IsOrImplements(typeof(IEnumerable)));
    }

    [Fact]
    public void IsOrImplements_RecognizesExactOpenGenericInterface()
    {
        Assert.True(typeof(IAsyncEnumerable<>).IsOrImplements(typeof(IAsyncEnumerable<>)));
    }

    [Fact]
    public void IsOrImplements_RecognizesOpenGenericInterfaceImplementation()
    {
        Assert.True(typeof(AsyncSequence).IsOrImplements(typeof(IAsyncEnumerable<>)));
    }

    [Fact]
    public void IsOrImplements_RecognizesInheritedOpenGenericInterfaceImplementation()
    {
        Assert.True(typeof(DerivedAsyncSequence).IsOrImplements(typeof(IAsyncEnumerable<>)));
    }

    [Fact]
    public void IsOrImplements_RecognizesClosedGenericInterfaceImplementation()
    {
        Assert.True(typeof(List<string>).IsOrImplements(typeof(IEnumerable<string>)));
    }

    [Fact]
    public void IsOrImplements_RejectsDifferentClosedGenericInterface()
    {
        Assert.False(typeof(List<int>).IsOrImplements(typeof(IEnumerable<string>)));
    }

    [Theory]
    [MemberData(nameof(InvalidArguments))]
    public void IsOrImplements_ReturnsFalseForInvalidArguments(Type? candidate, Type? interfaceType)
    {
        Assert.False(candidate.IsOrImplements(interfaceType));
    }

    public static TheoryData<Type?, Type?> InvalidArguments => new()
    {
        { null, typeof(IEnumerable) },
        { typeof(List<int>), null },
        { typeof(List<int>), typeof(List<int>) }
    };

    private class AsyncSequence : IAsyncEnumerable<int>
    {
        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class DerivedAsyncSequence : AsyncSequence;
}
