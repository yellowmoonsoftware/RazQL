using System.Collections;
using System.Linq.Expressions;
using RazQL.Template;

namespace RazQL.Tests.Template;

public class QueryDescriptorTests
{
    [Fact]
    public void ForExpression_CreatesDescriptorFromScalarMethodGroup()
    {
        Expression<Func<IAttributedMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            mapper => mapper.LoadAsync;

        var descriptor = QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(expression);
        QueryDescriptor metadata = descriptor;

        Assert.IsType<QueryDescriptor<IAttributedMapper, string, DescriptorResult>>(descriptor);
        Assert.Same(descriptor, metadata);
        Assert.Equal(typeof(IAttributedMapper), descriptor.MapperType);
        Assert.Equal(typeof(IAttributedMapper).GetMethod(nameof(IAttributedMapper.LoadAsync)), descriptor.QueryMethod);
        Assert.Equal(typeof(string), descriptor.CriteriaType);
        Assert.Equal(typeof(DescriptorResult), descriptor.ResultType);
        Assert.Equal("Fetch", descriptor.TemplateName);
        Assert.Equal(["Artists"], descriptor.GetQueryGroupCandidates());
        Assert.Equal(["Fetch"], descriptor.GetQueryNameCandidates());
    }

    [Fact]
    public void ForExpression_CreatesDescriptorFromEnumerableMethodGroup()
    {
        Expression<Func<IResultMapper,
            Func<string, CancellationToken, Task<IEnumerable<DescriptorResult>>>>> expression =
            mapper => mapper.LoadManyAsync;

        var descriptor = QueryDescriptor.ForExpression<IResultMapper, string, DescriptorResult>(expression);

        Assert.Equal(typeof(IResultMapper), descriptor.MapperType);
        Assert.Equal(typeof(IResultMapper).GetMethod(nameof(IResultMapper.LoadManyAsync)), descriptor.QueryMethod);
        Assert.Equal(typeof(string), descriptor.CriteriaType);
        Assert.IsType<QueryDescriptor<IResultMapper, string, IEnumerable<DescriptorResult>>>(descriptor);
        Assert.Equal(typeof(IEnumerable<DescriptorResult>), descriptor.ResultType);
    }

    [Fact]
    public void QuerySourceLoaderType_UsesMethodAttributeType()
    {
        Expression<Func<IAttributedMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            mapper => mapper.LoadAsync;

        var descriptor = QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(expression);

        Assert.Equal(typeof(FileSystemTemplateSourceLoader), descriptor.QuerySourceLoaderType);
    }

    [Fact]
    public void QuerySourceLoaderType_DefaultsToResourceLoader()
    {
        Expression<Func<IAttributedMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            mapper => mapper.FindAsync;

        var descriptor = QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(expression);

        Assert.Equal(typeof(ResourceTemplateSourceLoader), descriptor.QuerySourceLoaderType);
    }

    [Fact]
    public void TemplateSourceConfiguration_MethodValuesOverrideInterfaceValuesIndependently()
    {
        var locationOverride = QueryDescriptor.ForExpression<ITemplateConfigurationMapper, string, DescriptorResult>(
            mapper => mapper.OverrideLocationAsync);
        var loaderOverride = QueryDescriptor.ForExpression<ITemplateConfigurationMapper, string, DescriptorResult>(
            mapper => mapper.OverrideLoaderAsync);

        Assert.Equal(typeof(FileSystemTemplateSourceLoader), locationOverride.QuerySourceLoaderType);
        Assert.Equal("Method", locationOverride.TemplateLocation);
        Assert.Equal(typeof(ResourceTemplateSourceLoader), loaderOverride.QuerySourceLoaderType);
        Assert.Equal("Interface", loaderOverride.TemplateLocation);
    }

    [Fact]
    public void TemplateSourceConfiguration_BlankMethodLocationDefersToInterfaceLocation()
    {
        var descriptor = QueryDescriptor.ForExpression<ITemplateConfigurationMapper, string, DescriptorResult>(
            mapper => mapper.BlankLocationAsync);

        Assert.Equal(typeof(FileSystemTemplateSourceLoader), descriptor.QuerySourceLoaderType);
        Assert.Equal("Interface", descriptor.TemplateLocation);
    }

    [Fact]
    public void TemplateSourceConfiguration_BlankInterfaceLocationIsNotSpecified()
    {
        var descriptor = QueryDescriptor.ForExpression<IBlankTemplateLocationMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);

        Assert.Equal(typeof(ResourceTemplateSourceLoader), descriptor.QuerySourceLoaderType);
        Assert.Null(descriptor.TemplateLocation);
    }

    [Fact]
    public void TemplateSourceConfiguration_UsesConfigurationFromDerivedMethodAttribute()
    {
        var descriptor = QueryDescriptor.ForExpression<IDerivedMethodTemplateSourceMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);

        Assert.Equal(typeof(FileSystemTemplateSourceLoader), descriptor.QuerySourceLoaderType);
        Assert.Equal("Derived", descriptor.TemplateLocation);
        Assert.Equal("Lookup", descriptor.TemplateName);
        Assert.Equal(["Lookup"], descriptor.GetQueryNameCandidates());
    }

    [Fact]
    public void TemplateSourceConfiguration_UsesDerivedAttributeOnMapperInterface()
    {
        var descriptor = QueryDescriptor.ForExpression<IDerivedTemplateSourceAttributeMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);

        Assert.Equal(typeof(FileSystemTemplateSourceLoader), descriptor.QuerySourceLoaderType);
    }

    [Fact]
    public void TemplateSourceConfiguration_RejectsMultipleAttributesOnMapperInterface()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            QueryDescriptor.ForExpression<IConflictingTemplateSourceMapper, string, DescriptorResult>(
                mapper => mapper.FindAsync));

        Assert.Contains(nameof(TemplateSourceLoaderAttribute), exception.Message);
        Assert.Contains(nameof(IConflictingTemplateSourceMapper), exception.Message);
    }

    [Fact]
    public void TemplateSourceConfiguration_RejectsMultipleAttributesOnMapperMethod()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            QueryDescriptor.ForExpression<IConflictingTemplateSourceMethodMapper, string, DescriptorResult>(
                mapper => mapper.FindAsync));

        Assert.Contains(nameof(TemplateSourceLoaderAttribute), exception.Message);
        Assert.Contains(nameof(IConflictingTemplateSourceMethodMapper.FindAsync), exception.Message);
    }

    [Theory]
    [InlineData(nameof(IAttributedMapper.FindAsync), "Find", "FindAsync")]
    [InlineData(nameof(IAttributedMapper.Synchronously), "Synchronously", null)]
    public void GetQueryNameCandidates_UsesMethodNameAndRemovesAsyncSuffix(
        string methodName,
        string primaryCandidate,
        string? fallbackCandidate)
    {
        var descriptor = DescriptorFor(methodName);
        var expected = fallbackCandidate is null
            ? new[] { primaryCandidate }
            : new[] { primaryCandidate, fallbackCandidate };

        Assert.Equal(expected, descriptor.GetQueryNameCandidates());
    }

    [Fact]
    public void GetQueryGroupCandidates_RemovesConventionalInterfacePrefixAndRetainsFullName()
    {
        Expression<Func<IConventionMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            mapper => mapper.FindAsync;

        var descriptor = QueryDescriptor.ForExpression<IConventionMapper, string, DescriptorResult>(expression);

        Assert.Equal(["ConventionMapper", "IConventionMapper"], descriptor.GetQueryGroupCandidates());
    }

    [Fact]
    public void GetQueryGroupCandidates_DoesNotRemoveNonconventionalPrefix()
    {
        var descriptor = QueryDescriptor.ForExpression<InventoryMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);

        Assert.Equal(["InventoryMapper"], descriptor.GetQueryGroupCandidates());
    }

    [Fact]
    public void ForExpression_EquivalentDescriptorsRemainEqual()
    {
        var first = QueryDescriptor.ForExpression<IConventionMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);
        var second = QueryDescriptor.ForExpression<IConventionMapper, string, DescriptorResult>(
            mapper => mapper.FindAsync);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void ResultType_PreservesAllowedEnumerableScalarTypes()
    {
        var textDescriptor = QueryDescriptor.ForExpression<IResultMapper, string, string>(
            mapper => mapper.LoadTextAsync);
        var bytesDescriptor = QueryDescriptor.ForExpression<IResultMapper, string, byte[]>(
            mapper => mapper.LoadBytesAsync);

        Assert.Equal(typeof(string), textDescriptor.ResultType);
        Assert.Equal(typeof(byte[]), bytesDescriptor.ResultType);
    }

    [Fact]
    public void ForExpression_RejectsUnsupportedEnumerableResults()
    {
        AssertUnsupportedResult<List<DescriptorResult>>(
            mapper => mapper.ListAsync,
            "IEnumerable type in the form");
        AssertUnsupportedResult<DescriptorResult[]>(
            mapper => mapper.ArrayAsync,
            "IEnumerable type in the form");
        AssertUnsupportedResult<IAsyncEnumerable<DescriptorResult>>(
            mapper => mapper.AsyncEnumerableAsync,
            "IAsyncEnumerable<T>");
        AssertUnsupportedResult<CustomAsyncEnumerable>(
            mapper => mapper.CustomAsyncEnumerableAsync,
            "IAsyncEnumerable<T>");
        AssertUnsupportedResult<IAsyncEnumerator<DescriptorResult>>(
            mapper => mapper.AsyncEnumeratorAsync,
            "IAsyncEnumerator<T>");
        AssertUnsupportedResult<CustomAsyncEnumerator>(
            mapper => mapper.CustomAsyncEnumeratorAsync,
            "IAsyncEnumerator<T>");
        AssertUnsupportedResult<IEnumerator>(
            mapper => mapper.EnumeratorAsync,
            "IEnumerator implementing types");
        AssertUnsupportedResult<IEnumerator<DescriptorResult>>(
            mapper => mapper.GenericEnumeratorAsync,
            "IEnumerator implementing types");
        AssertUnsupportedResult<IEnumerable<IEnumerable<DescriptorResult>>>(
            mapper => mapper.NestedEnumerableAsync,
            "IEnumerable type in the form");
    }

    [Fact]
    public void ForExpression_RejectsTaskLikeMappedResults()
    {
        AssertUnsupportedResult<Task<DescriptorResult>>(
            mapper => mapper.NestedGenericTaskAsync,
            "cannot be Task");
        AssertUnsupportedResult<Task>(
            mapper => mapper.NestedTaskAsync,
            "cannot be Task");
        AssertUnsupportedResult<ValueTask<DescriptorResult>>(
            mapper => mapper.NestedGenericValueTaskAsync,
            "cannot be Task");
        AssertUnsupportedResult<ValueTask>(
            mapper => mapper.NestedValueTaskAsync,
            "cannot be Task");
        AssertUnsupportedResult<IEnumerable<Task<DescriptorResult>>>(
            mapper => mapper.NestedTaskCollectionAsync,
            "cannot be Task");
    }

    [Fact]
    public void ForExpression_RejectsSequenceMethodPresentedAsScalarResult()
    {
        Expression<Func<IResultMapper, Func<string, CancellationToken, Task<IEnumerable<DescriptorResult>>>>> expression =
            mapper => mapper.LoadManyAsync;

        var exception = Assert.Throws<ArgumentException>(() =>
            QueryDescriptor.ForExpression<IResultMapper, string, IEnumerable<DescriptorResult>>(expression));

        Assert.Equal("expression", exception.ParamName);
        Assert.Contains("does not match", exception.Message);
    }

    [Fact]
    public void ForExpression_RejectsCriteriaVarianceThatDoesNotMatchTheMethod()
    {
        Expression<Func<IContravariantCriteriaMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            mapper => mapper.FindAsync;

        var exception = Assert.Throws<ArgumentException>(() =>
            QueryDescriptor.ForExpression<IContravariantCriteriaMapper, string, DescriptorResult>(expression));

        Assert.Equal("expression", exception.ParamName);
        Assert.Contains("does not match", exception.Message);
    }

    [Fact]
    public void ForExpression_RejectsCapturedInstanceMethodGroup()
    {
        IAttributedMapper capturedMapper = null!;
        Expression<Func<IAttributedMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            _ => capturedMapper.FindAsync;

        var exception = Assert.Throws<ArgumentException>(
            () => QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(expression));

        Assert.Equal("expression", exception.ParamName);
        Assert.Contains("directly from the mapper parameter", exception.Message);
    }

    [Fact]
    public void ForExpression_RejectsStaticMethodGroup()
    {
        Expression<Func<IStaticMethodMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            _ => IStaticMethodMapper.LoadStaticAsync;

        var exception = Assert.Throws<ArgumentException>(
            () => QueryDescriptor.ForExpression<IStaticMethodMapper, string, DescriptorResult>(expression));

        Assert.Equal("expression", exception.ParamName);
        Assert.Contains("instance mapper method", exception.Message);
    }

    [Fact]
    public void ForExpression_RejectsConcreteMapperType()
    {
        Expression<Func<MethodGroupMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            mapper => mapper.LoadAsync;

        var exception = Assert.Throws<ArgumentException>(
            () => QueryDescriptor.ForExpression<MethodGroupMapper, string, DescriptorResult>(expression));

        Assert.Equal("mapperType", exception.ParamName);
        Assert.Contains("must be an interface", exception.Message);
    }

    [Fact]
    public void ForExpression_RejectsMapperInterfaceWithoutDirectAttribute()
    {
        Expression<Func<IDerivedUndecoratedMapper,
            Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            mapper => mapper.LoadAsync;

        var exception = Assert.Throws<ArgumentException>(
            () => QueryDescriptor.ForExpression<IDerivedUndecoratedMapper, string, DescriptorResult>(expression));

        Assert.Equal("mapperType", exception.ParamName);
        Assert.Contains(nameof(RazQLMapperAttribute), exception.Message);
    }

    [Fact]
    public void ForExpression_AcceptsMethodDeclaredByBaseMapperInterface()
    {
        Expression<Func<IDerivedMethodGroupMapper, Func<string, CancellationToken, Task<DescriptorResult>>>> expression =
            mapper => mapper.LoadAsync;

        var descriptor = QueryDescriptor.ForExpression<IDerivedMethodGroupMapper, string, DescriptorResult>(expression);

        Assert.Equal(typeof(IDerivedMethodGroupMapper), descriptor.MapperType);
        Assert.Equal(typeof(IBaseMethodGroupMapper<string, DescriptorResult>), descriptor.QueryMethod.DeclaringType);
        Assert.Equal(["DerivedMethodGroupMapper", "IDerivedMethodGroupMapper"],
            descriptor.GetQueryGroupCandidates());
    }

    [Fact]
    public void ForExpression_HiddenMethodDoesNotUseBaseMethodAttributes()
    {
        var descriptor = QueryDescriptor.ForExpression<IHiddenMethodMapper, string, DescriptorResult>(
            mapper => mapper.LoadAsync);

        Assert.Equal(typeof(IHiddenMethodMapper), descriptor.QueryMethod.DeclaringType);
        Assert.Equal(["Load", "LoadAsync"], descriptor.GetQueryNameCandidates());
        Assert.Equal(typeof(ResourceTemplateSourceLoader), descriptor.QuerySourceLoaderType);
    }

    private static QueryDescriptor DescriptorFor(string methodName) => methodName switch
    {
        nameof(IAttributedMapper.FindAsync) =>
            QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(mapper => mapper.FindAsync),
        nameof(IAttributedMapper.Synchronously) =>
            QueryDescriptor.ForExpression<IAttributedMapper, string, DescriptorResult>(mapper => mapper.Synchronously),
        _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, null)
    };

    private static void AssertUnsupportedResult<TResult>(
        Expression<Func<IInvalidResultMapper, Func<string, CancellationToken, Task<TResult>>>> expression,
        string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => QueryDescriptor.ForExpression<IInvalidResultMapper, string, TResult>(expression));

        Assert.Equal("queryMethod", exception.ParamName);
        Assert.Contains(expectedMessage, exception.Message);
    }
}

[RazQLMapper(Name = "Artists")]
public interface IAttributedMapper
{
    [RazQLQueryTemplateSource(
        typeof(FileSystemTemplateSourceLoader),
        TemplateLocation = "  ",
        TemplateName = "Fetch")]
    Task<DescriptorResult> LoadAsync(string criteria, CancellationToken cancellationToken);

    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);

    Task<DescriptorResult> Synchronously(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
public interface IConventionMapper
{
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
[RazQLTemplateSource(
    typeof(FileSystemTemplateSourceLoader),
    TemplateLocation = " Interface ")]
public interface ITemplateConfigurationMapper
{
    [RazQLQueryTemplateSource(TemplateLocation = " Method ")]
    Task<DescriptorResult> OverrideLocationAsync(string criteria, CancellationToken cancellationToken);

    [RazQLQueryTemplateSource(typeof(ResourceTemplateSourceLoader))]
    Task<DescriptorResult> OverrideLoaderAsync(string criteria, CancellationToken cancellationToken);

    [RazQLQueryTemplateSource(TemplateLocation = "  ")]
    Task<DescriptorResult> BlankLocationAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
[RazQLTemplateSource(TemplateLocation = "  ")]
public interface IBlankTemplateLocationMapper
{
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class FileSystemQuerySourceAttribute()
    : TemplateSourceLoaderAttribute(typeof(FileSystemTemplateSourceLoader));

[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class ResourceQuerySourceAttribute()
    : TemplateSourceLoaderAttribute(typeof(ResourceTemplateSourceLoader));

[RazQLMapper]
[FileSystemQuerySource]
public interface IDerivedTemplateSourceAttributeMapper
{
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DerivedMethodTemplateSourceAttribute()
    : RazQLQueryTemplateSourceAttribute(typeof(FileSystemTemplateSourceLoader))
{
    public DerivedMethodTemplateSourceAttribute(string templateName) : this()
    {
        TemplateName = templateName;
        TemplateLocation = "Derived";
    }
}

[RazQLMapper]
public interface IDerivedMethodTemplateSourceMapper
{
    [DerivedMethodTemplateSource("Lookup")]
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
[FileSystemQuerySource]
[ResourceQuerySource]
public interface IConflictingTemplateSourceMapper
{
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
public interface IConflictingTemplateSourceMethodMapper
{
    [RazQLQuery("select 1")]
    [RazQLQueryTemplateSource(typeof(FileSystemTemplateSourceLoader))]
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
public interface InventoryMapper
{
    Task<DescriptorResult> FindAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
public interface IResultMapper
{
    Task<IEnumerable<DescriptorResult>> LoadManyAsync(string criteria, CancellationToken cancellationToken);
    Task<string> LoadTextAsync(string criteria, CancellationToken cancellationToken);
    Task<byte[]> LoadBytesAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
public interface IInvalidResultMapper
{
    Task<List<DescriptorResult>> ListAsync(string criteria, CancellationToken cancellationToken);
    Task<DescriptorResult[]> ArrayAsync(string criteria, CancellationToken cancellationToken);
    Task<IAsyncEnumerable<DescriptorResult>> AsyncEnumerableAsync(string criteria, CancellationToken cancellationToken);
    Task<CustomAsyncEnumerable> CustomAsyncEnumerableAsync(string criteria, CancellationToken cancellationToken);
    Task<IAsyncEnumerator<DescriptorResult>> AsyncEnumeratorAsync(string criteria, CancellationToken cancellationToken);
    Task<CustomAsyncEnumerator> CustomAsyncEnumeratorAsync(string criteria, CancellationToken cancellationToken);
    Task<IEnumerator> EnumeratorAsync(string criteria, CancellationToken cancellationToken);
    Task<IEnumerator<DescriptorResult>> GenericEnumeratorAsync(string criteria, CancellationToken cancellationToken);
    Task<IEnumerable<IEnumerable<DescriptorResult>>> NestedEnumerableAsync(string criteria, CancellationToken cancellationToken);
    Task<Task<DescriptorResult>> NestedGenericTaskAsync(string criteria, CancellationToken cancellationToken);
    Task<Task> NestedTaskAsync(string criteria, CancellationToken cancellationToken);
    Task<ValueTask<DescriptorResult>> NestedGenericValueTaskAsync(string criteria, CancellationToken cancellationToken);
    Task<ValueTask> NestedValueTaskAsync(string criteria, CancellationToken cancellationToken);
    Task<IEnumerable<Task<DescriptorResult>>> NestedTaskCollectionAsync(string criteria, CancellationToken cancellationToken);
}

public sealed class CustomAsyncEnumerable : IAsyncEnumerable<DescriptorResult>
{
    public IAsyncEnumerator<DescriptorResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}

public sealed class CustomAsyncEnumerator : IAsyncEnumerator<DescriptorResult>
{
    public DescriptorResult Current => throw new NotSupportedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(false);
}

public sealed class MethodGroupMapper
{
    public Task<DescriptorResult> LoadAsync(string criteria, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

[RazQLMapper]
public interface IStaticMethodMapper
{
    static Task<DescriptorResult> LoadStaticAsync(string criteria, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}

public interface IBaseMethodGroupMapper<in TCriteria, TResult>
{
    Task<TResult> LoadAsync(TCriteria criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
public interface IDerivedMethodGroupMapper : IBaseMethodGroupMapper<string, DescriptorResult>;

[RazQLMapper]
public interface IBaseDecoratedMapper
{
    Task<DescriptorResult> LoadAsync(string criteria, CancellationToken cancellationToken);
}

public interface IDerivedUndecoratedMapper : IBaseDecoratedMapper;

public interface IBaseAttributedMethodMapper
{
    [RazQLQueryTemplateSource(
        typeof(FileSystemTemplateSourceLoader),
        TemplateName = "BaseLoad")]
    Task<DescriptorResult> LoadAsync(string criteria, CancellationToken cancellationToken);
}

[RazQLMapper]
public interface IHiddenMethodMapper : IBaseAttributedMethodMapper
{
    new Task<DescriptorResult> LoadAsync(string criteria, CancellationToken cancellationToken);
}

public sealed record DescriptorResult;

[RazQLMapper]
public interface IContravariantCriteriaMapper
{
    Task<DescriptorResult> FindAsync(object criteria, CancellationToken cancellationToken);
}
