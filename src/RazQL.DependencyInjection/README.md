# RazQL.DependencyInjection

`RazQL.DependencyInjection` integrates the RazQL runtime and generated mapper implementations with Microsoft dependency injection.

## Registration

Install the standard package and the optional Microsoft dependency-injection integration:

```shell
dotnet package add RazQL
dotnet package add RazQL.DependencyInjection
```

Register a provider-specific `DbDataSource`, then add RazQL and the assembly containing generated mappers:

```csharp
using RazQL.Dapper;

services.AddSingleton<DbDataSource>(providerSpecificDataSource);

services.AddRazQL(builder => builder
    .UsingExecutionAdapter<DapperExecutionAdapter>()
    .AddMappersFromAssembly(typeof(IArtistMapper).Assembly));
```

`AddRazQL` requires an execution adapter selected in its builder action. It registers the Razor engine, template cache, SQL generator, query executor, data-binder factories, immutable data-binder options, built-in template source loaders, and loader resolver as singletons. Generated mapper implementations are registered against their mapper interfaces and as `IMapperTemplatePreloader` instances.

`AddRazQL` calls `AddLogging()` so the default services also resolve from a bare `ServiceCollection`. It does not select an output provider. Configure logging through the host or with `AddLogging`; existing logger registrations remain in effect.

## Customization

The builder provides explicit replacements for each runtime service:

```csharp
services.AddRazQL(builder => builder
    .WithTemplateCache<CustomTemplateCache>()
    .WithQueryExecutor<CustomQueryExecutor>()
    .UsingExecutionAdapter<CustomExecutionAdapter>()
    .AddTemplateSourceLoader<ApiTemplateSourceLoader>()
    .AddMappersFromAssembly(typeof(IArtistMapper).Assembly));
```

Configure trusted `ORDER BY` fragments during registration:

```csharp
services.AddRazQL(builder => builder
    .UsingExecutionAdapter<DapperExecutionAdapter>()
    .ConfigureDataBinding(options => options
        .WithOrderByDirectionClause(OrderByDirection.Asc, "ASC")));
```

The completed `DataBinderOptions` is an immutable singleton. Each query gets a new binder, parameter collection, and parameter-name provider. You can also bind `DataBinderOptions` from `IConfiguration` and pass the result to `ConfigureDataBinding(options)`. The builder can replace `IDataBinderContextFactory` or `IParameterNameProviderFactory`.

All RazQL services and loaders are registered as singletons. Implementations must therefore be thread-safe and must not depend on scoped services.

`AddMappersFromAssembly` reads assembly metadata emitted by `RazQL.Generators`; it does not scan mapper interfaces or template files at runtime.

## Preloading

Generated mappers implement `IMapperTemplatePreloader`. A startup coordinator can resolve all preloaders, call `PreloadTemplates`, and await the resulting tasks to compile templates and surface failures before serving requests.
