# RazQL

RazQL (pronounced “rascal”) builds and executes parameterized SQL from trusted Razor templates. This package combines the provider-neutral runtime, compile-time mapper validation and source generation, and the Dapper execution adapter.

## Installation

```shell
dotnet package add RazQL
```

Add the optional Microsoft dependency-injection integration separately:

```shell
dotnet package add RazQL.DependencyInjection
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
