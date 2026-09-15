using System.Reflection;
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

    /// <summary>Replaces the default singleton Dapper executor.</summary>
    /// <typeparam name="T">The Dapper executor implementation.</typeparam>
    /// <returns>This builder.</returns>
    IRazQLBuilder WithDapperExecutor<T>() where T : IDapperExecutor;

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
}
