using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RazorEngineCore;
using RazQL.Execution;
using RazQL.Template;

namespace RazQL.DependencyInjection;

/// <summary>Provides RazQL registration extensions for Microsoft dependency injection.</summary>
public static class RazQLServiceExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds RazQL services, source loaders, and optionally generated mappers.</summary>
        /// <param name="builderAction">An optional callback that customizes registrations.</param>
        /// <returns>The service collection.</returns>
        public IServiceCollection AddRazQL(Action<IRazQLBuilder>? builderAction = null)
        {
            var razQLBuilder = new RazQLBuilder();
            builderAction?.Invoke(razQLBuilder);

            services.TryAddSingleton<IRazorEngine>(_ => new RazorEngine());

            // Add RazQL services
            services.Add(razQLBuilder.ServiceDescriptors.Values);
            foreach (var (mapperType, implementationType) in razQLBuilder.MapperImplementations)
            {
                services.AddSingleton(mapperType, implementationType);
                services.AddSingleton(
                    typeof(IMapperTemplatePreloader),
                    provider => (IMapperTemplatePreloader)provider.GetRequiredService(mapperType));
            }
            // Add defined ITemplateSourceLoaders
            foreach (var templateSourceLoader in razQLBuilder.TemplateSourceLoaders)
            {
                services.AddSingleton(templateSourceLoader);
                services.AddSingleton(typeof(ITemplateSourceLoader), p => p.GetRequiredService(templateSourceLoader));
            }
            return services;
        }
    }

    private sealed class RazQLBuilder : IRazQLBuilder
    {
        internal IDictionary<Type, ServiceDescriptor> ServiceDescriptors { get; } = new []
            {
                new ServiceDescriptor(typeof(ITemplateCache), typeof(DefaultTemplateCache), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(ISqlGenerator), typeof(SqlGenerator), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IQueryExecutor), typeof(QueryExecutor), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IDapperExecutor), typeof(DapperExecutor), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(ITemplateSourceLoaderResolver), typeof(DefaultTemplateSourceLoaderResolver), ServiceLifetime.Singleton)
            }.ToDictionary(sd => sd.ServiceType);

        internal ISet<Type> TemplateSourceLoaders { get; } = new HashSet<Type>
        {
            typeof(FileSystemTemplateSourceLoader), typeof(ResourceTemplateSourceLoader),
            typeof(QueryAttributeTemplateSourceLoader)
        };

        internal IDictionary<Type, Type> MapperImplementations { get; } = new Dictionary<Type, Type>();

        private void AddService(ServiceDescriptor serviceDescriptor) => ServiceDescriptors[serviceDescriptor.ServiceType] = serviceDescriptor;

        public IRazQLBuilder WithTemplateCache<T>() where T : ITemplateCache
        {
            AddService(new ServiceDescriptor(typeof(ITemplateCache), typeof(T), ServiceLifetime.Singleton));
            return this;
        }

        public IRazQLBuilder WithSqlGenerator<T>() where T : ISqlGenerator
        {
            AddService(new ServiceDescriptor(typeof(ISqlGenerator), typeof(T), ServiceLifetime.Singleton));
            return this;
        }

        public IRazQLBuilder WithQueryExecutor<T>() where T : IQueryExecutor
        {
            AddService(new ServiceDescriptor(typeof(IQueryExecutor), typeof(T), ServiceLifetime.Singleton));
            return this;
        }

        public IRazQLBuilder WithDapperExecutor<T>() where T : IDapperExecutor
        {
            AddService(new ServiceDescriptor(typeof(IDapperExecutor), typeof(T), ServiceLifetime.Singleton));
            return this;
        }

        public IRazQLBuilder WithTemplateSourceLoaderResolver<T>() where T : ITemplateSourceLoaderResolver
        {
            AddService(new ServiceDescriptor(typeof(ITemplateSourceLoaderResolver), typeof(T), ServiceLifetime.Singleton));
            return this;
        }

        public IRazQLBuilder AddTemplateSourceLoader<T>() where T : ITemplateSourceLoader
        {
            TemplateSourceLoaders.Add(typeof(T));
            return this;
        }

        public IRazQLBuilder AddMappersFromAssembly(Assembly assembly)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            foreach (var mapping in assembly.GetCustomAttributes<RazQLMapperImplementationAttribute>())
            {
                MapperImplementations[mapping.MapperType] = mapping.ImplementationType;
            }

            return this;
        }

    }
}
