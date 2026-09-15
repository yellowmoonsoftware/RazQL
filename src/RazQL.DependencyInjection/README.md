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
services.AddSingleton<DbDataSource>(providerSpecificDataSource);

services.AddRazQL(builder =>
    builder.AddMappersFromAssembly(typeof(IArtistMapper).Assembly));
```

`AddRazQL` registers the Razor engine, template cache, SQL generator, query executor, Dapper adapter, built-in template source loaders, and loader resolver as singletons. Generated mapper implementations are registered against their mapper interfaces and as `IMapperTemplatePreloader` instances.

Applications using `ILogger<T>` registrations supplied by a .NET host can observe template compilation and generated-SQL diagnostics.

## Customization

The builder provides explicit replacements for each runtime service:

```csharp
services.AddRazQL(builder => builder
    .WithTemplateCache<CustomTemplateCache>()
    .WithQueryExecutor<CustomQueryExecutor>()
    .AddTemplateSourceLoader<ApiTemplateSourceLoader>()
    .AddMappersFromAssembly(typeof(IArtistMapper).Assembly));
```

All RazQL services and loaders are registered as singletons. Implementations must therefore be thread-safe and must not depend on scoped services.

`AddMappersFromAssembly` reads assembly metadata emitted by `RazQL.Generators`; it does not scan mapper interfaces or template files at runtime.

## Preloading

Generated mappers implement `IMapperTemplatePreloader`. A startup coordinator can resolve all preloaders, call `PreloadTemplates`, and await the resulting tasks to compile templates and surface failures before serving requests.
