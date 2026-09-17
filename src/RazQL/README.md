# RazQL.Core

`RazQL.Core` is the provider-neutral runtime library for generating parameterized SQL from trusted Razor templates. It is included by the standard `RazQL` package. Dapper execution is provided separately by `RazQL.Dapper`.

## Core Pipeline

`QueryDescriptor<TMapper, TCriteria, TResult>` identifies a mapper method, its criteria, the full value type returned by its task, selected source loader, and template naming candidates. For `Task<IEnumerable<TElement>>`, its result type is `IEnumerable<TElement>`. Its non-generic `QueryDescriptor` base exposes metadata to source loaders; the typed descriptor flows through cache, SQL generation, and execution. Create one with `QueryDescriptor.ForExpression<TMapper, TCriteria, TElement>(mapper => mapper.FindAsync)`. The default runtime pipeline:

1. Resolves source through `ITemplateSourceLoaderResolver`.
2. Compiles and caches the Razor template through `ITemplateCache`.
3. Evaluates it through `ISqlGenerator`, collecting provider-neutral parameters.
4. Opens a connection from `DbDataSource` and executes through `IQueryExecutor` and the selected `IExecutionAdapter`.

Successful template compilations remain cached. Failed or canceled source loading and compilation are evicted, so a later request can retry. The package does not create a provider-specific `DbDataSource`; the application must supply one.

## Template Conventions

Resource loading is the default. A mapper method such as `IArtistMapper.FindByIdAsync` searches these conventional names:

```text
{MapperNamespace}.SqlTemplates.ArtistMapper.FindById.sql.cshtml
{MapperNamespace}.SqlTemplates.ArtistMapper.FindByIdAsync.sql.cshtml
{MapperNamespace}.SqlTemplates.IArtistMapper.FindById.sql.cshtml
{MapperNamespace}.SqlTemplates.IArtistMapper.FindByIdAsync.sql.cshtml
```

Configure a mapper or method when conventions are insufficient:

```csharp
[RazQLMapper]
[RazQLTemplateSource(TemplateLocation = "Data.Access")]
public interface IArtistMapper
{
    [RazQLQueryTemplateSource(TemplateName = "Find")]
    Task<Artist?> FindByIdAsync(long id, CancellationToken cancellationToken);
}
```

`TemplateLocation` is inserted before `SqlTemplates`. Resource loaders interpret it as a namespace fragment; the file-system loader interprets it as a path relative to `AppContext.BaseDirectory`.

For a short query, use inline template source:

```csharp
[RazQLQuery("select id, name from artist where id = @Model.Bind()")]
Task<Artist?> FindByIdAsync(long id, CancellationToken cancellationToken);
```

## Deployment Compatibility

Native AOT is unsupported: `ITemplateCache` compiles Razor templates through
RazorEngineCore at runtime, even when applications preload them at startup.
Native AOT does not support the required runtime code generation and dynamic
assembly loading.

Single-file publishing without trimming is distinct from Native AOT. A narrow
.NET 10 framework-dependent smoke test of RazorEngineCore 2026.1.1 succeeded
on macOS x64, but the full RazQL pipeline has not been validated in that mode.
Self-contained single-file publishing and trimming remain untested. Applications
using the file-system loader must deploy their SQL template files separately
under `AppContext.BaseDirectory`.

## Binding Values

Templates derive from `RazQLModel<TCriteria>`. Bind data instead of writing model values directly into SQL:

```razor
@inherits RazQL.RazQLModel<ArtistCriteria>
select id, name
from artist
where 1 = 1
@if (!IsNullOrEmpty(m => m.Name))
{
    @: and name ilike @Bind(m => m.Name, Like)
}
```

The binder supports scalar values, transformed values, array parameters, null checks, collection iteration, and enum-to-SQL `ORDER BY` mapping. Column expressions supplied to `OrderBy` must come from trusted application code.

Razor templates are compiled and executed as .NET code. Never accept template source from an untrusted user or external system without an appropriate security boundary.
