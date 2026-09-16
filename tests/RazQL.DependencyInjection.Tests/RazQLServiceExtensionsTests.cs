using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RazQL;
using RazQL.Binding;
using RazQL.Execution;
using RazQL.Template;

namespace RazQL.DependencyInjection.Tests;

public class RazQLServiceExtensionsTests
{
    [Fact]
    public void AddRazQL_RequiresExecutionAdapter()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddRazQL());

        Assert.Contains("UsingExecutionAdapter", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddRazQL_RequiresExecutionAdapterAfterBuilderAction()
    {
        var services = new ServiceCollection();
        var actionInvoked = false;

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddRazQL(builder =>
        {
            actionInvoked = true;
            builder.ConfigureDataBinding(_ => { });
        }));

        Assert.True(actionInvoked);
        Assert.Contains("UsingExecutionAdapter", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddRazQL_RequiresBuilderSelectionEvenWhenAdapterIsAlreadyRegistered()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IExecutionAdapter, ReplacementExecutionAdapter>();

        var exception = Assert.Throws<InvalidOperationException>(() => services.AddRazQL());

        Assert.Contains("UsingExecutionAdapter", exception.Message);
        Assert.Single(services);
    }

    [Fact]
    public void UsingExecutionAdapter_RegistersSelectedAdapter()
    {
        var services = new ServiceCollection();
        services.AddRazQL(builder => builder.UsingExecutionAdapter<ReplacementExecutionAdapter>());
        using var provider = services.BuildServiceProvider();

        Assert.IsType<ReplacementExecutionAdapter>(provider.GetRequiredService<IExecutionAdapter>());
    }

    [Fact]
    public void AddRazQL_RegistersBuiltInLoadersAsSharedSingletons()
    {
        var services = new ServiceCollection();
        services.AddRazQL(builder => builder.UsingExecutionAdapter<ReplacementExecutionAdapter>());
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
            .UsingExecutionAdapter<ReplacementExecutionAdapter>()
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
        services.AddRazQL(builder => builder
            .UsingExecutionAdapter<ReplacementExecutionAdapter>()
            .WithTemplateSourceLoaderResolver<ReplacementSourceLoaderResolver>());
        using var provider = services.BuildServiceProvider();

        Assert.IsType<ReplacementSourceLoaderResolver>(
            provider.GetRequiredService<ITemplateSourceLoaderResolver>());
    }

    [Fact]
    public void AddRazQL_RegistersBindingFactoriesAndConfiguredImmutableOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILogger<SqlGenerator>>(NullLogger<SqlGenerator>.Instance);
        services.AddSingleton<ILogger<DefaultTemplateCache>>(NullLogger<DefaultTemplateCache>.Instance);
        services.AddRazQL(builder => builder
            .UsingExecutionAdapter<ReplacementExecutionAdapter>()
            .ConfigureDataBinding(options => options.WithOrderByDirectionClause(OrderByDirection.Asc, "UP"))
            .ConfigureDataBinding(options => options.WithOrderByNullsClause(OrderByNulls.Last, "AT END")));
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<DataBinderOptions>();
        Assert.Same(options, provider.GetRequiredService<DataBinderOptions>());
        Assert.Equal("UP", options.OrderByDirectionClause[OrderByDirection.Asc]);
        Assert.Equal("DESC", options.OrderByDirectionClause[OrderByDirection.Desc]);
        Assert.Equal("AT END", options.OrderByNullsClause[OrderByNulls.Last]);
        Assert.IsType<DefaultDataBinderContextFactory>(provider.GetRequiredService<IDataBinderContextFactory>());
        Assert.IsType<DefaultParameterNameProviderFactory>(provider.GetRequiredService<IParameterNameProviderFactory>());
        Assert.IsType<SqlGenerator>(provider.GetRequiredService<ISqlGenerator>());
    }

    [Fact]
    public void ConfigureDataBinding_AcceptsExistingOptions()
    {
        var options = new DataBinderOptions(
            new Dictionary<OrderByDirection, string> { [OrderByDirection.Asc] = "UP" },
            DataBinderOptions.DefaultOrderByNullsClause);
        var services = new ServiceCollection();
        services.AddRazQL(builder => builder
            .UsingExecutionAdapter<ReplacementExecutionAdapter>()
            .ConfigureDataBinding(options));
        using var provider = services.BuildServiceProvider();

        var registeredOptions = provider.GetRequiredService<DataBinderOptions>();
        Assert.Equal("UP", registeredOptions.OrderByDirectionClause[OrderByDirection.Asc]);
        Assert.False(registeredOptions.OrderByDirectionClause.ContainsKey(OrderByDirection.Desc));
    }

    [Fact]
    public void ConfigureDataBinding_AcceptsOptionsBoundFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataBinder:OrderByDirectionClause:Asc"] = "UP"
            })
            .Build();
        var options = configuration.GetSection("DataBinder").Get<DataBinderOptions>();
        Assert.NotNull(options);
        var services = new ServiceCollection();
        services.AddRazQL(builder => builder
            .UsingExecutionAdapter<ReplacementExecutionAdapter>()
            .ConfigureDataBinding(options));
        using var provider = services.BuildServiceProvider();

        var registered = provider.GetRequiredService<DataBinderOptions>();
        Assert.Equal("UP", registered.OrderByDirectionClause[OrderByDirection.Asc]);
        Assert.Equal("DESC", registered.OrderByDirectionClause[OrderByDirection.Desc]);
    }

    [Fact]
    public void BindingFactoryOverrides_ReplaceDefaultRegistrations()
    {
        var services = new ServiceCollection();
        services.AddRazQL(builder => builder
            .UsingExecutionAdapter<ReplacementExecutionAdapter>()
            .WithDataBinderContextFactory<ReplacementDataBinderContextFactory>()
            .WithParameterNameProviderFactory<ReplacementParameterNameProviderFactory>());
        using var provider = services.BuildServiceProvider();

        Assert.IsType<ReplacementDataBinderContextFactory>(
            provider.GetRequiredService<IDataBinderContextFactory>());
        Assert.IsType<ReplacementParameterNameProviderFactory>(
            provider.GetRequiredService<IParameterNameProviderFactory>());
    }

    [Fact]
    public void AddMappersFromAssembly_RegistersGeneratedMappersAndPreloadersAsSharedSingletons()
    {
        var services = new ServiceCollection();
        services.AddRazQL(builder => builder
            .UsingExecutionAdapter<ReplacementExecutionAdapter>()
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

public sealed class ReplacementDataBinderContextFactory : IDataBinderContextFactory
{
    public DataBinderContext<TCriteria> Create<TCriteria>(TCriteria criteria) =>
        throw new NotSupportedException();
}

public sealed class ReplacementParameterNameProviderFactory : IParameterNameProviderFactory
{
    public IParameterNameProvider Create() => throw new NotSupportedException();
}

public sealed class ReplacementExecutionAdapter : IExecutionAdapter
{
    public Task<IEnumerable<TResult>> QueryAsync<TResult>(DbConnection connection,
        ParameterizedQueryResult parameterizedQueryResult, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<TResult?> QuerySingleOrDefaultAsync<TResult>(DbConnection connection,
        ParameterizedQueryResult parameterizedQueryResult, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
