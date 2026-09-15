using Microsoft.Extensions.DependencyInjection;
using RazQL;
using RazQL.Execution;
using RazQL.Template;

namespace RazQL.DependencyInjection.Tests;

public class RazQLServiceExtensionsTests
{
    [Fact]
    public void AddRazQL_RegistersBuiltInLoadersAsSharedSingletons()
    {
        var services = new ServiceCollection();
        services.AddRazQL();
        using var provider = services.BuildServiceProvider();

        var loaders = provider.GetServices<ITemplateSourceLoader>().ToArray();

        Assert.Equal(3, loaders.Length);
        Assert.Same(
            provider.GetRequiredService<FileSystemTemplateSourceLoader>(),
            Assert.Single(loaders, loader => loader is FileSystemTemplateSourceLoader));
        Assert.Same(
            provider.GetRequiredService<ResourceTemplateSourceLoader>(),
            Assert.Single(loaders, loader => loader is ResourceTemplateSourceLoader));
        Assert.Same(
            provider.GetRequiredService<QueryAttributeTemplateSourceLoader>(),
            Assert.Single(loaders, loader => loader is QueryAttributeTemplateSourceLoader));
    }

    [Fact]
    public void AddTemplateSourceLoader_RegistersMultipleLoadersAsSharedSingletons()
    {
        var services = new ServiceCollection();
        services.AddRazQL(builder => builder
            .AddTemplateSourceLoader<FirstCustomSourceLoader>()
            .AddTemplateSourceLoader<SecondCustomSourceLoader>());
        using var provider = services.BuildServiceProvider();

        var loaders = provider.GetServices<ITemplateSourceLoader>().ToArray();

        Assert.Equal(5, loaders.Length);
        Assert.Same(
            provider.GetRequiredService<FirstCustomSourceLoader>(),
            Assert.Single(loaders, loader => loader is FirstCustomSourceLoader));
        Assert.Same(
            provider.GetRequiredService<SecondCustomSourceLoader>(),
            Assert.Single(loaders, loader => loader is SecondCustomSourceLoader));
    }

    [Fact]
    public void WithTemplateSourceLoaderResolver_ReplacesDefaultResolver()
    {
        var services = new ServiceCollection();
        services.AddRazQL(builder =>
            builder.WithTemplateSourceLoaderResolver<ReplacementSourceLoaderResolver>());
        using var provider = services.BuildServiceProvider();

        Assert.IsType<ReplacementSourceLoaderResolver>(
            provider.GetRequiredService<ITemplateSourceLoaderResolver>());
    }

    [Fact]
    public void AddMappersFromAssembly_RegistersGeneratedMappersAndPreloadersAsSharedSingletons()
    {
        var services = new ServiceCollection();
        services.AddRazQL(builder => builder
            .WithQueryExecutor<TestQueryExecutor>()
            .AddMappersFromAssembly(typeof(ITestMapper).Assembly));
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<ITestMapper>();
        var second = provider.GetRequiredService<ITestMapper>();
        var preloader = Assert.Single(provider.GetServices<IMapperTemplatePreloader>());

        Assert.Same(first, second);
        Assert.Same(first, preloader);
        Assert.True(first.GetType().IsSealed);
        Assert.True(first.GetType().IsNotPublic);
    }
}

[RazQLMapper]
public interface ITestMapper
{
    Task<string?> FindAsync(int id, CancellationToken cancellationToken);
}

public sealed class FirstCustomSourceLoader : ITemplateSourceLoader
{
    public Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken) =>
        Task.FromResult("first");
}

public sealed class SecondCustomSourceLoader : ITemplateSourceLoader
{
    public Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken) =>
        Task.FromResult("second");
}

public sealed class ReplacementSourceLoaderResolver : ITemplateSourceLoaderResolver
{
    public ITemplateSourceLoader Resolve(QueryDescriptor queryDescriptor) =>
        throw new NotSupportedException();
}

public sealed class TestQueryExecutor : IQueryExecutor
{
    public Task<IEnumerable<TResult>> ExecuteAsync<TCriteria, TResult>(
        QueryDescriptor descriptor,
        TCriteria criteria,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<TResult?> ExecuteSingleOrDefaultAsync<TCriteria, TResult>(
        QueryDescriptor descriptor,
        TCriteria criteria,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
