# RazQL

RazQL (pronounced “rascal”) is an experimental .NET library for building and executing parameterized SQL from Razor templates. It takes inspiration from MyBatis mapper interfaces while using source generation, RazorEngineCore, and Dapper.

The project is being prepared for an initial open source release. No stable NuGet package has been published yet, and the public API may change before version 1.0.

## Projects

- `RazQL` is the standard metapackage and installs the runtime together with source generation.
- `RazQL.Core` contains mapper attributes, template loading and compilation, safe parameter binding, SQL generation, and query execution.
- `RazQL.Generators` validates mapper interfaces and emits their implementations during compilation.
- `RazQL.DependencyInjection` registers the runtime pipeline and generated mappers with Microsoft dependency injection.

## Installation

Install the standard package to reference the runtime and activate the generator:

```shell
dotnet package add RazQL
```

Add Microsoft dependency-injection integration separately:

```shell
dotnet package add RazQL.DependencyInjection
```

## Mapper Example

```csharp
using RazQL;

[RazQLMapper]
public interface IArtistMapper
{
    Task<Artist?> FindByIdAsync(
        long id,
        CancellationToken cancellationToken = default);
}
```

The conventional embedded-resource template is located relative to the mapper source file:

```text
SqlTemplates/ArtistMapper/FindById.sql.cshtml
```

```razor
@inherits RazQL.RazQLModel<long>
select id, name
from artist
where id = @Model.Bind()
```

Register the generated mapper and a provider-specific `DbDataSource`:

```csharp
services.AddRazQL(builder =>
    builder.AddMappersFromAssembly(typeof(IArtistMapper).Assembly));
```

Template values should be emitted through `Bind`, `BindAsArray`, and the other data-binder helpers. Razor template source is compiled and executed as .NET code and must therefore be treated as trusted application code.

## Template Sources

Embedded resources are the default. RazQL resolves them using:

```text
{MapperNamespace}.[TemplateLocation.]SqlTemplates.{Mapper}.{Query}.sql.cshtml
```

Use `RazQLTemplateSourceAttribute` on an interface for shared configuration and `RazQLQueryTemplateSourceAttribute` on a method for an override. `RazQLQueryAttribute` is available for short inline templates. File-system templates are resolved under `AppContext.BaseDirectory` and must remain inside that directory.

## Build and Test

The repository requires the .NET 10 SDK.

```shell
dotnet restore RazQL.slnx
dotnet build RazQL.slnx
dotnet test RazQL.slnx
```

## Status

RazQL is a proof of concept moving toward its first public release. Package metadata, compatibility targets, and release automation still need to be finalized.

## License

RazQL is available under the [MIT License](LICENSE).
