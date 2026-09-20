using System.Reflection;
using RazQL.Binding;
using RazQL.Execution;
using RazQL.Template;

namespace RazQL.DependencyInjection;

/// <summary>Configures the services registered by <c>AddRazQL</c>.</summary>
public interface IRazQLBuilder
{
    /// <summary>Replaces the default singleton template cache.</summary>
    /// <typeparam name="T">The template cache implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder WithTemplateCache<T>() where T : ITemplateCache;

    /// <summary>Replaces the default singleton SQL generator.</summary>
    /// <typeparam name="T">The SQL generator implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder WithSqlGenerator<T>() where T : ISqlGenerator;

    /// <summary>Replaces the default singleton query executor.</summary>
    /// <typeparam name="T">The query executor implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder WithQueryExecutor<T>() where T : IQueryExecutor;

    /// <summary>Selects the singleton adapter used to execute generated queries.</summary>
    /// <typeparam name="T">The execution adapter implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder UsingExecutionAdapter<T>() where T : IExecutionAdapter;

    /// <summary>Replaces the default singleton template source-loader resolver.</summary>
    /// <typeparam name="T">The resolver implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder WithTemplateSourceLoaderResolver<T>() where T : ITemplateSourceLoaderResolver;

    /// <summary>Adds a singleton template source loader to the resolver's available loaders.</summary>
    /// <typeparam name="T">The concrete loader implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder AddTemplateSourceLoader<T>() where T : ITemplateSourceLoader;

    /// <summary>Registers generated mapper implementations declared by an assembly.</summary>
    /// <param name="assembly">The assembly containing generated mapper registration attributes.</param>
    /// <returns>This builder.</returns>
    IRazQLBuilder AddMappersFromAssembly(Assembly assembly);

    /// <summary>
    /// Adds a hosted service that loads and compiles every registered generated mapper template during host startup.
    /// </summary>
    /// <returns>This builder.</returns>
    IRazQLBuilder PreloadTemplatesOnStartup();

    /// <summary>Configures immutable data-binder options before services are registered.</summary>
    /// <param name="builderAction">An action that customizes SQL clause mappings.</param>
    /// <returns>This builder.</returns>
    IRazQLBuilder ConfigureDataBinding(Action<IDataBinderOptionsBuilder> builderAction);

    /// <summary>Applies an existing immutable data-binder options value.</summary>
    /// <param name="options">The options to apply.</param>
    /// <returns>This builder.</returns>
    IRazQLBuilder ConfigureDataBinding(DataBinderOptions options);

    /// <summary>Replaces the default singleton data-binder context factory.</summary>
    /// <typeparam name="T">The factory implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder WithDataBinderContextFactory<T>() where T : IDataBinderContextFactory;

    /// <summary>Replaces the default singleton parameter-name provider factory.</summary>
    /// <typeparam name="T">The factory implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder WithParameterNameProviderFactory<T>() where T : IParameterNameProviderFactory;
}
