# RazQL

RazQL (pronounced “rascal”) is an experimental .NET library for building and executing parameterized SQL from Razor templates. It takes inspiration from MyBatis mapper interfaces while using source generation, [RazorEngineCore](https://github.com/adoconnection/RazorEngineCore), and [Dapper](https://github.com/DapperLib/Dapper).

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
@inherits RazQL.RazQLModel<long, Artist>
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

## Association Mapping

Templates can describe single-object associations and child collections alongside their SQL.
The second type argument of `RazQLModel<TCriteria, TResult>` is the mapped **row object**,
including when the mapper method returns `Task<IEnumerable<TResult>>`.

For example, an album can contain songs, contributing artists, and a primary artist:

```csharp
public sealed class Album
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public ICollection<Song> Songs { get; } = new List<Song>();
    public Artist? PrimaryArtist { get; set; }
}

public sealed class Song
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public ICollection<Artist> Artists { get; } = new List<Artist>();
}

public sealed record Artist(long Id, string Name);
```

The mapper method can return `Task<IEnumerable<Album>>` and accept `ICollection<long>`:

```razor
@using RazQL.Binding
@using Example.Models
@inherits RazQL.RazQLModel<ICollection<long>, Album>
@{
    ResultMap(album => album
        .Identity(value => value.Id)
        .Collection<Song>("id", value => value.Songs, song => song
            .Identity(value => value.Id)
            .Collection<Artist>("id", value => value.Artists, artist => artist
                .Identity(value => value.Id)))
        .Association<Artist?>("id", value => value.PrimaryArtist));
}
select album.id, album.title,
       song.id, song.title,
       contributor.id, contributor.name,
       primary_artist.id, primary_artist.name
from album
left join song on song.album_id = album.id
left join song_artist on song_artist.song_id = song.id
left join artist contributor on contributor.id = song_artist.artist_id
left join artist primary_artist on primary_artist.id = album.primary_artist_id
where album.id = any(@Model.BindAsArray())
```

The column groups follow the relationship tree in depth-first order: album, song,
song artist, then primary artist. The string passed to `Collection` or `Association`
identifies the first column in a child group for Dapper's `splitOn`; it does not define identity.
For optional joined objects, that first column must be null when the object is absent.
Missing children are omitted from collections by default. Collections must be initialized
by the DTO, and single-object association properties need setters.

Register `Identity` once per mapped type anywhere in the tree. The provider is shared by
every occurrence of that type in the query. An artist referenced by `PrimaryArtist` and a
song's artist collection therefore uses the same in-memory instance when its identity matches.
Composite identities can use tuples, for example `.Identity(value => (value.TenantId, value.Id))`.
Types without an identity provider use their DTO equality and hash code; custom DTO overrides
are optional. Identity equality and hash codes must remain stable during mapping.

The Dapper adapter assembles all joined rows before returning results. Repeated root identities
produce one result, and single-result queries check the number of distinct roots rather than
the number of joined rows. Queries without relationships use ordinary Dapper execution and
do not deduplicate rows merely because an identity was configured. Object caches belong to
one execution, so subsequent queries create independent objects.

See the [Dapper mapping details and current limitations](src/RazQL.Dapper/README.md#association-mapping).

## Template Sources

Embedded resources are the default. RazQL resolves them using:

```text
{MapperNamespace}.[TemplateLocation.]SqlTemplates.{Mapper}.{Query}.sql.cshtml
```

Use `RazQLTemplateSourceAttribute` on an interface for shared configuration and `RazQLQueryTemplateSourceAttribute` on a method for an override. `RazQLQueryAttribute` is available for short inline templates. File-system templates are resolved under `AppContext.BaseDirectory` and must remain inside that directory.

## Deployment Compatibility

RazQL does not support Native AOT. It compiles Razor SQL templates through
RazorEngineCore at runtime, including when templates are preloaded at startup;
Native AOT does not support runtime code generation or dynamic assembly loading.

Single-file publishing is a separate deployment mode. RazorEngineCore 2026.1.1
compiled and ran a template in a .NET 10, macOS x64, framework-dependent
single-file smoke test with trimming disabled. That does not establish support
for the full RazQL stack. Self-contained single-file publishing and trimming
have not been validated. File-system templates must still be deployed as
separate content under `AppContext.BaseDirectory`; embedded-resource templates
avoid that external-file requirement.

## Build and Test

The repository requires .NET SDK 10.0.100 or newer within the .NET 10 line. See the [generator compatibility policy](src/RazQL.Generators/README.md#compiler-compatibility).

```shell
dotnet restore RazQL.slnx
dotnet build RazQL.slnx
dotnet test RazQL.slnx
./eng/test-packages.sh
```

## Status

RazQL is a proof of concept moving toward its first public release. Release Please prepares a single version and changelog for all five packages, starting at `0.1.0-alpha`. CI builds and tests on .NET 10 and verifies a clean package-only consumer. Tagged GitHub releases publish the verified packages and runtime symbol packages to NuGet.org through Trusted Publishing. Source Link metadata connects the published symbols to the matching repository commit.

## Community

See [CONTRIBUTING.md](CONTRIBUTING.md) for development and pull request guidance,
[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) for participation standards, and
[CHANGELOG.md](CHANGELOG.md) for changes. Report vulnerabilities privately as
described in [SECURITY.md](SECURITY.md).

## License

RazQL is available under the [MIT License](LICENSE).
