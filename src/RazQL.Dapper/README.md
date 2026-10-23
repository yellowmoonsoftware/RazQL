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
canonical child objects to a collection, optionally creating it through a configured factory.
Nested relationships are traversed depth-first.
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
encountered repeatedly. Collection properties can be initialized by the DTO or an optional
`collectionFactory` on either `Collection` overload:

```csharp
.Collection<Song>("id", value => value.Songs,
    song => song.Identity(value => value.Id),
    collectionFactory: () => new List<Song>())
```

The factory is invoked only when the canonical owner's collection is null. Its result is assigned
to the property and reused for subsequent children. The factory must return a non-null collection
assignable to the declared property type, and that property must have a setter when initialization
is required. Each owner and property can receive its own collection by returning a new instance.
Existing collections are preserved without invoking the factory; a getter-only property returning
a mutable `ICollection<T>` remains supported.

Initialization also occurs for missing children: the collection stays empty unless
`addNullChildren: true` permits a null entry. A null collection without a factory throws an
`InvalidOperationException` naming the owner type and property. Association properties require a setter.

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

### Relationship Context

Collection deduplication distinguishes the canonical owner, the mapped property, and the canonical
child. An artist can appear once in each of two collections. Repeated rows, including different
mapping paths reaching the same owner and collection property, do not add the same child again.
Missing collection children follow the same per-property tracking rules.

Single-object associations use **last assignment wins**. Every mapped occurrence assigns its
canonical child, including null, without recording assignment history or checking for conflicts.
For example, `A`, `B`, `A` leaves `A`; `A`, null leaves null; and null, `A`, null leaves null.
DTO constructor or property-initializer values are overwritten by the first mapped assignment.
The same canonical artist can still populate both `PrimaryArtist` and `SecondaryArtist` on one owner.

Queries should normally agree on one association identity for each owner and property. If values
conflict, the last assignment follows result-row order and, within a row, depth-first result-map
order. Specify SQL ordering when relying on row order. This property-assignment policy is separate
from object identity: the first object encountered for an identity still supplies its scalar values.

### Mapping Scope and Cost

RazQL does not provide configurable association conflict policies or ORM-style change tracking.
Graph mapping buffers joined rows and retains canonical objects and collection-membership entries
for the execution. Large joins that multiply rows can consume substantial memory. Flat queries
avoid graph assembly and its tracking overhead.
