# RazQL

RazQL (pronounced “rascal”) builds and executes parameterized SQL from trusted Razor templates. This package combines the provider-neutral runtime, compile-time mapper validation and source generation, and the Dapper execution adapter.

## Installation

```shell
dotnet package add RazQL --prerelease
```

Add the optional Microsoft dependency-injection integration separately:

```shell
dotnet package add RazQL.DependencyInjection --prerelease
```

## Mapper Example

```csharp
using RazQL;

[RazQLMapper]
public interface IArtistMapper
{
    [RazQLQuery("select id, name from artist where id = @Model.Bind()")]
    Task<Artist?> FindByIdAsync(
        long id,
        CancellationToken cancellationToken = default);
}
```

The generator validates decorated mapper interfaces and emits their implementations during compilation. The runtime compiles trusted Razor templates and collects provider-neutral parameters. Select `DapperExecutionAdapter` when configuring `RazQL.DependencyInjection`; the adapter converts those parameters for Dapper and executes the resulting SQL.

Razor templates execute as application code and must not contain untrusted source.

## Deployment Compatibility

Native AOT is not supported because RazQL compiles Razor templates at runtime,
including during startup preloading. Single-file publishing is different: a
.NET 10 framework-dependent, untrimmed smoke test passed with RazorEngineCore
2026.1.1 on macOS x64, but the complete RazQL stack has not been validated.
Self-contained single-file publishing and trimming are also untested. If you
use file-system templates, deploy them as separate files alongside the app;
embedded-resource templates do not require separate template files.
