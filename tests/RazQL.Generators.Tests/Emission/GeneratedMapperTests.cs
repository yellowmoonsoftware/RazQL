using System.Reflection;
using NSubstitute;
using RazQL;
using RazQL.Execution;
using RazQL.Template;
using RazorEngineCore;

namespace RazQL.Generators.Tests.Emission;

public class GeneratedMapperTests
{
    [Fact]
    public void Generator_EmitsDiscoverableInternalSealedImplementation()
    {
        var mapping = GetMapping<ITestMapper>();

        Assert.Equal(typeof(ITestMapper), mapping.MapperType);
        Assert.True(mapping.ImplementationType.IsNotPublic);
        Assert.True(mapping.ImplementationType.IsSealed);
        Assert.Equal("RazQL.Generators.Tests.Emission.Generated", mapping.ImplementationType.Namespace);
        Assert.True(typeof(ITestMapper).IsAssignableFrom(mapping.ImplementationType));
        Assert.True(typeof(IMapperTemplatePreloader).IsAssignableFrom(mapping.ImplementationType));
        Assert.NotNull(mapping.ImplementationType.GetConstructor([typeof(IQueryExecutor)]));
    }

    [Fact]
    public async Task GeneratedMapper_DelegatesQueryUsingGeneratedDescriptor()
    {
        var criteria = new TestCriteria();
        var cancellationToken = new CancellationToken(canceled: false);
        var expected = new[] { new TestResult(1, "Test Result") };
        var executor = Substitute.For<IQueryExecutor>();
        executor.ExecuteAsync<ITestMapper, TestCriteria, TestResult>(
                Arg.Any<QueryDescriptor<ITestMapper, TestCriteria, IEnumerable<TestResult>>>(),
                criteria,
                cancellationToken)
            .Returns(expected);
        var mapper = CreateMapper<ITestMapper>(executor);

        var result = await mapper.FindAsync(criteria, cancellationToken);

        Assert.Same(expected, result);
        await executor.Received(1).ExecuteAsync<ITestMapper, TestCriteria, TestResult>(
            Arg.Is<QueryDescriptor<ITestMapper, TestCriteria, IEnumerable<TestResult>>>(descriptor =>
                descriptor.MapperType == typeof(ITestMapper) &&
                descriptor.QueryMethod.Name == nameof(ITestMapper.FindAsync)),
            criteria,
            cancellationToken);
    }

    [Fact]
    public async Task GeneratedMapper_DelegatesSingleResultUsingOverloadedExecutor()
    {
        var criteria = new TestCriteria();
        var cancellationToken = new CancellationToken(canceled: false);
        var expected = new TestResult(1, "Test Result");
        var executor = Substitute.For<IQueryExecutor>();
        executor.ExecuteAsync<ISingleTestMapper, TestCriteria, TestResult?>(
                Arg.Any<QueryDescriptor<ISingleTestMapper, TestCriteria, TestResult?>>(),
                criteria,
                cancellationToken)
            .Returns(expected);
        var mapper = CreateMapper<ISingleTestMapper>(executor);

        var result = await mapper.FindOneAsync(criteria, cancellationToken);

        Assert.Same(expected, result);
        await executor.Received(1).ExecuteAsync<ISingleTestMapper, TestCriteria, TestResult?>(
            Arg.Is<QueryDescriptor<ISingleTestMapper, TestCriteria, TestResult?>>(descriptor =>
                descriptor.MapperType == typeof(ISingleTestMapper) &&
                descriptor.QueryMethod.Name == nameof(ISingleTestMapper.FindOneAsync)),
            criteria,
            cancellationToken);
    }

    [Fact]
    public async Task GeneratedMapper_ReturnsTemplateCacheTaskWithoutAwaitingIt()
    {
        var cancellationToken = new CancellationToken(canceled: false);
        var executor = Substitute.For<IQueryExecutor>();
        var templateCache = Substitute.For<ITemplateCache>();
        var compiledTemplate = Substitute.For<IRazorEngineCompiledTemplate<RazQLModel<TestCriteria>>>();
        var compilation = new TaskCompletionSource<IRazorEngineCompiledTemplate<RazQLModel<TestCriteria>>>();
        templateCache.GetTemplateAsync<ITestMapper, TestCriteria, IEnumerable<TestResult>>(
                Arg.Any<QueryDescriptor<ITestMapper, TestCriteria, IEnumerable<TestResult>>>(),
                cancellationToken)
            .Returns(compilation.Task);
        var mapper = CreateMapper<ITestMapper>(executor);

        var tasks = ((IMapperTemplatePreloader)mapper)
            .PreloadTemplates(templateCache, cancellationToken)
            .ToArray();

        Assert.Same(compilation.Task, Assert.Single(tasks));
        Assert.False(tasks[0].IsCompleted);
        compilation.SetResult(compiledTemplate);
        await tasks[0];
        await templateCache.Received(1).GetTemplateAsync<ITestMapper, TestCriteria, IEnumerable<TestResult>>(
            Arg.Is<QueryDescriptor<ITestMapper, TestCriteria, IEnumerable<TestResult>>>(descriptor =>
                descriptor.MapperType == typeof(ITestMapper) &&
                descriptor.QueryMethod.Name == nameof(ITestMapper.FindAsync)),
            cancellationToken);
    }

    private static TMapper CreateMapper<TMapper>(IQueryExecutor executor)
    {
        var mapping = GetMapping<TMapper>();
        return Assert.IsAssignableFrom<TMapper>(
            Activator.CreateInstance(mapping.ImplementationType, executor));
    }

    private static RazQLMapperImplementationAttribute GetMapping<TMapper>()
    {
        return typeof(ITestMapper).Assembly
            .GetCustomAttributes<RazQLMapperImplementationAttribute>()
            .Single(mapping => mapping.MapperType == typeof(TMapper));
    }
}

[RazQLMapper]
public interface ITestMapper
{
    Task<IEnumerable<TestResult>> FindAsync(
        TestCriteria criteria,
        CancellationToken cancellationToken);
}

[RazQLMapper]
public interface ISingleTestMapper
{
    [RazQLQuery("select 1")]
    Task<TestResult?> FindOneAsync(TestCriteria criteria, CancellationToken cancellationToken);
}

public sealed record TestCriteria;

public sealed record TestResult(long Id, string Name);
