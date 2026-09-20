using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RazorEngineCore;
using RazQL.Binding;
using RazQL.Execution;
using RazQL.Template;

namespace RazQL.DependencyInjection;

/// <summary>Provides RazQL registration extensions for Microsoft dependency injection.</summary>
public static class RazQLServiceExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>Adds RazQL services, logging, source loaders, and optionally generated mappers.</summary>
        /// <param name="builderAction">Configures the required execution adapter and optional registrations.</param>
        /// <returns>The service collection.</returns>
        public IServiceCollection AddRazQL(Action<IRazQLBuilder>? builderAction = null)
        {
            var razQLBuilder = new RazQLBuilder();
            builderAction?.Invoke(razQLBuilder);
            if (!razQLBuilder.ServiceDescriptors.ContainsKey(typeof(IExecutionAdapter)))
            {
                throw new InvalidOperationException(
                    "An execution adapter must be configured with UsingExecutionAdapter<T>() before registering RazQL services.");
            }

            services.AddLogging();
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

            if (razQLBuilder.ShouldPreloadTemplatesOnStartup)
            {
                services.TryAddEnumerable(
                    ServiceDescriptor.Singleton<IHostedService, TemplatePreloaderHostedService>());
            }

            // Add defined ITemplateSourceLoaders
            foreach (var templateSourceLoader in razQLBuilder.TemplateSourceLoaders)
            {
                services.AddSingleton(templateSourceLoader);
                services.AddSingleton(typeof(ITemplateSourceLoader), p => p.GetRequiredService(templateSourceLoader));
            }

            // Configure DataBinderOptions
            var dataBinderOptionsBuilder = new DataBinderOptionsBuilder();
            razQLBuilder.DataBinderOptionsBuilderAction?.Invoke(dataBinderOptionsBuilder);
            services.AddSingleton(dataBinderOptionsBuilder.Build());

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
                new ServiceDescriptor(typeof(IDataBinderContextFactory), typeof(DefaultDataBinderContextFactory), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(IParameterNameProviderFactory), typeof(DefaultParameterNameProviderFactory), ServiceLifetime.Singleton),
                new ServiceDescriptor(typeof(ITemplateSourceLoaderResolver), typeof(DefaultTemplateSourceLoaderResolver), ServiceLifetime.Singleton)
            }.ToDictionary(sd => sd.ServiceType);

        internal ISet<Type> TemplateSourceLoaders { get; } = new HashSet<Type>
        {
            typeof(FileSystemTemplateSourceLoader), typeof(ResourceTemplateSourceLoader),
            typeof(QueryAttributeTemplateSourceLoader)
        };

        internal IDictionary<Type, Type> MapperImplementations { get; } = new Dictionary<Type, Type>();

        internal bool ShouldPreloadTemplatesOnStartup { get; private set; }

        internal Action<IDataBinderOptionsBuilder>? DataBinderOptionsBuilderAction { get; private set; }

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

        public IRazQLBuilder UsingExecutionAdapter<T>() where T : IExecutionAdapter
        {
            AddService(new ServiceDescriptor(typeof(IExecutionAdapter), typeof(T), ServiceLifetime.Singleton));
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

        public IRazQLBuilder PreloadTemplatesOnStartup()
        {
            ShouldPreloadTemplatesOnStartup = true;
            return this;
        }

        public IRazQLBuilder WithDataBinderContextFactory<T>() where T : IDataBinderContextFactory
        {
            AddService(new ServiceDescriptor(typeof(IDataBinderContextFactory), typeof(T), ServiceLifetime.Singleton));
            return this;
        }

        public IRazQLBuilder WithParameterNameProviderFactory<T>() where T : IParameterNameProviderFactory
        {
            AddService(new ServiceDescriptor(typeof(IParameterNameProviderFactory), typeof(T), ServiceLifetime.Singleton));
            return this;
        }

        public IRazQLBuilder ConfigureDataBinding(Action<IDataBinderOptionsBuilder> builderAction)
        {
            ArgumentNullException.ThrowIfNull(builderAction);
            DataBinderOptionsBuilderAction += builderAction;
            return this;
        }

        public IRazQLBuilder ConfigureDataBinding(DataBinderOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            ConfigureDataBinding(builder => builder.WithOptions(options));
            return this;
        }
    }
}
