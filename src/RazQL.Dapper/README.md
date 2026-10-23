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

### Dapper Compatibility for Relations and Multi-Mapping

**Dapper 2.1.89 is the validated baseline for this RazQL.Dapper version.** Relation mapping calls
Dapper's non-public arbitrary-type `MultiMapAsync` through a cached delegate. The public `Type[]`
overload in that version accepts SQL rather than a `CommandDefinition`; the private bridge preserves
command options, including cancellation. Non-public APIs are not covered by Dapper's public API
compatibility guarantees, so the bridge checks the required method signature at runtime.

The bridge resolves on the first relation query and validates the full parameter and return types
and generic constraints. If no unique compatible method is found, or delegate binding fails, it
throws `NotSupportedException` identifying the loaded assembly and its informational and assembly
versions, the expected method signature, the validated Dapper baseline, and recovery steps.
It does not silently fall back to execution that loses command options. Flat queries without
relationships use ordinary Dapper calls and do not initialize the private bridge.

**Consumers may use a newer Dapper version.** The NuGet dependency specifies a minimum version,
and RazQL does not enforce a version allowlist. Relation mapping proceeds when the bridge finds
and binds the required method; consumers do not need to wait for RazQL to validate their chosen
version. The signature check establishes structural compatibility, not identical behavior behind
an unchanged signature, so consumers should test their queries when upgrading Dapper.

Applications that prefer to stay on the validated baseline can optionally use an exact reference:

```xml
<PackageReference Include="Dapper" Version="[2.1.89]" />
```

For applications using central package management, put the exact range in `PackageVersion` instead.
If a dependency requires a newer version, resolve that requirement explicitly rather than assuming
an exact reference can override it. When reporting a compatibility issue, include the resolved
Dapper package version (`dotnet list package --include-transitive`) and the full exception.

Before adopting another Dapper version as the supported baseline, update `Directory.Packages.props`
and run the solution suite and package-consumption check. The suite must pass the bridge's positive
and incompatible-signature tests, adapter tests for graph mapping and cancellation, and PostgreSQL
relationship integration tests. Keep the version named in this policy, the XML documentation, and
the failure message aligned with the validated dependency. CI runs these checks against the
repository's selected baseline; RazQL does not maintain a Dapper version matrix. This maintainer
validation process does not restrict consumers from choosing newer versions. This policy applies
to both `Association` and `Collection` mappings.

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
