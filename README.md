# RazQL

RazQL (pronounced “rascal”) is an experimental .NET library for building and executing parameterized SQL from Razor templates. It takes inspiration from MyBatis mapper interfaces while using source generation, RazorEngineCore, and Dapper.

The project is being prepared for an initial open source release. No stable NuGet package has been published yet, and the public API may change before version 1.0.

## Projects

- `RazQL` is the standard metapackage and installs the runtime, source generation, and Dapper adapter.
- `RazQL.Core` contains mapper attributes, template loading and compilation, safe parameter binding, SQL generation, and query execution contracts without a Dapper dependency.
- `RazQL.Dapper` provides the Dapper execution adapter and parameter conversion.
- `RazQL.Generators` validates mapper interfaces and emits their implementations during compilation.
- `RazQL.DependencyInjection` registers the runtime pipeline and generated mappers with Microsoft dependency injection.

## Installation

Install the standard package to reference the runtime and activate the generator:

```shell
dotnet package add RazQL --prerelease
```

Add Microsoft dependency-injection integration separately:

```shell
dotnet package add RazQL.DependencyInjection --prerelease
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

Register the generated mapper, select the Dapper adapter, and supply a provider-specific `DbDataSource`:

```csharp
using RazQL.Dapper;

services.AddRazQL(builder => builder
    .UsingExecutionAdapter<DapperExecutionAdapter>()
    .AddMappersFromAssembly(typeof(IArtistMapper).Assembly));
```

Applications using another execution adapter can reference `RazQL.Core`, `RazQL.Generators`, and `RazQL.DependencyInjection` directly without installing Dapper.

Template values should be emitted through `Bind`, `BindAsArray`, and the other data-binder helpers. Razor template source is compiled and executed as .NET code and must therefore be treated as trusted application code.

## Template Sources

Embedded resources are the default. RazQL resolves them using:

```text
{MapperNamespace}.[TemplateLocation.]SqlTemplates.{Mapper}.{Query}.sql.cshtml
```

Use `RazQLTemplateSourceAttribute` on an interface for shared configuration and `RazQLQueryTemplateSourceAttribute` on a method for an override. `RazQLQueryAttribute` is available for short inline templates. File-system templates are resolved under `AppContext.BaseDirectory` and must remain inside that directory.

## Build and Test

The repository requires .NET SDK 10.0.100 or newer within the .NET 10 line. See the [generator compatibility policy](src/RazQL.Generators/README.md#compiler-compatibility).

```shell
dotnet restore RazQL.slnx
dotnet build RazQL.slnx
dotnet test RazQL.slnx
./eng/test-packages.sh
```

## Status

RazQL is a proof of concept moving toward its first public release. The first package version is `0.1.0-alpha`; release automation still needs to be finalized.

## Community

See [CONTRIBUTING.md](CONTRIBUTING.md) for development and pull request guidance,
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for participation standards, and
[CHANGELOG.md](CHANGELOG.md) for changes. Report vulnerabilities privately as
described in [SECURITY.md](SECURITY.md).

## License

RazQL is available under the [MIT License](LICENSE).
