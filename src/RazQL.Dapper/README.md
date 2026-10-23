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

## Association Mapping

The adapter interprets the relationship tree declared in `ResultMap(...)` and maps contiguous
column groups through Dapper. `Association` sets a single child property; `Collection` adds
canonical child objects to an existing collection. Nested relationships are traversed depth-first.
The [main README](../../README.md#association-mapping) includes matching DTOs and SQL.

### Object Identity

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

### Column Groups and Missing Children

Column names passed to `Association` and `Collection` identify Dapper's column split boundaries;
they do not define object identity. Selected column groups follow the result map in depth-first order.
Each group must contain the columns needed to construct its mapped object. For an optional child,
put a nullable joined key first so Dapper can recognize an absent object.

Missing children become null associations and are omitted from collections by default. The
`addNullChildren: true` overload permits one null entry for an owner when a missing child is
encountered repeatedly. Collection properties must be initialized by the DTO. A read-only property
returning a mutable `ICollection<T>` is sufficient; association properties require a setter.

### Execution and Compatibility

Dapper buffers the physical rows while the mapper assembles the graph. Callback results use an
explicit marker so a skipped duplicate root cannot be confused with a legitimate null or default
value. The adapter filters the buffered entries without maintaining a second result list.
`QuerySingleOrDefaultAsync` checks root cardinality after graph assembly, allowing multiple joined
rows for one root and throwing if more than one root is returned.

The adapter calls Dapper's private arbitrary-type `MultiMapAsync` through a cached delegate.
Dapper 2.1.86 exposes a public `Type[]` overload accepting SQL but not a `CommandDefinition`;
the bridge preserves cancellation and the other command options. Dapper upgrades must preserve
that private signature and should be validated with the adapter and integration tests.

### Current Limitations

Relationship tracking currently distinguishes owner-child pairs, but does not distinguish two
relationship properties on the same owner. Mapping the same child to both `PrimaryArtist` and
`SecondaryArtist` on one owner is not yet supported. Sharing an artist between an album property
and a song collection works because the owners differ.

Collection factories and configurable one-to-one conflict policies are not implemented. Initialize
collections yourself, and make joined rows agree on a single-object association's identity. These
constraints are separate from query-wide object identity.
