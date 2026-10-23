# RazQL.Dapper

`RazQL.Dapper` executes RazQL's generated SQL through Dapper. It depends on `RazQL.Core` and is included in the standard `RazQL` package. Consumers using a different execution adapter can reference `RazQL.Core` and `RazQL.Generators` without bringing in Dapper.

When using `RazQL.DependencyInjection`, select the adapter explicitly:

```csharp
using RazQL.Dapper;

services.AddRazQL(builder => builder
    .UsingExecutionAdapter<DapperExecutionAdapter>()
    .AddMappersFromAssembly(typeof(IArtistMapper).Assembly));
```

The application must also register a provider-specific `DbDataSource`.

## Object Identity

Result maps can define an identity selector on the root or a nested builder:

```razor
@inherits RazQL.RazQLModel<AlbumCriteria, Album>
@{
    ResultMap(album => album
        .Identity(value => value.Id)
        .Collection<Song>("id", value => value.Songs, song => song
            .Identity(value => value.Id)
            .Collection<Artist>("id", value => value.Artists, artist => artist
                .Identity(value => (value.TenantId, value.Id))))
        .Association<Artist?>("id", value => value.PrimaryArtist));
}
```

Register a selector once per mapped type. It applies everywhere that type occurs in the query,
regardless of where it is declared in the relationship tree. The artist in `PrimaryArtist` and
the artist in a song's collection share the same object instance when their identities match.
Identity is shared within one execution; separate executions have separate caches. The first
object encountered for an identity supplies its scalar values, and later rows contribute relationships.
Queries without relationships use ordinary Dapper execution; identity selectors do not deduplicate
their rows.

Selectors may return scalar or tuple keys. Each cache key captures the selected identity at construction;
hashing and equality compare that identity and the mapped type without invoking the selector again.
Selected identities must have stable equality and hash codes during mapping. Types without a selector
use their DTO equality and hash code, which must also remain stable. Null objects bypass selectors
and remain distinct from non-null objects whose
selected key is null. Duplicate selector registrations for the same type are rejected.

Column names passed to `Association` and `Collection` identify Dapper's column split boundaries;
they do not define object identity. Selected column groups follow the result map in depth-first order.
