using RazQL.IntegrationTests.Models;
using RazQL.IntegrationTests.Template;
using RazQL.Template;

namespace RazQL.IntegrationTests.Mappers;

[RazQLMapper]
public interface ITestArtistMapper
{
    [RazQLQuery("select id, name from gmdb.artist where name = @Model.Bind()")]
    Task<Artist?> FindWithInlineQueryAsync(string name, CancellationToken cancellationToken = default);

    [RazQLQuery("select id, name from gmdb.artist where name = @Model.Bind()")]
    Task<Artist> RequireArtistAsync(string name, CancellationToken cancellationToken = default);

    Task<IEnumerable<Artist>> FindWithResourceAsync(
        ArtistSearchCriteria criteria,
        CancellationToken cancellationToken = default);

    [RazQLQueryTemplateSource(typeof(FileSystemTemplateSourceLoader))]
    Task<IEnumerable<Artist>> FindWithFileSystemAsync(
        ICollection<string>? names,
        CancellationToken cancellationToken = default);

    [RazQLQueryTemplateSource(TemplateLocation = "Overrides", TemplateName = "LocatedArtist")]
    Task<Artist?> FindWithResourceOverridesAsync(
        string name,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<Artist>> SearchAsync(
        ArtistDynamicCriteria criteria,
        CancellationToken cancellationToken = default);

    [RazQLQueryTemplateSource(typeof(IntegrationTemplateSourceLoader))]
    Task<Artist?> FindWithCustomLoaderAsync(string name, CancellationToken cancellationToken = default);

    [RazQLQuery("select 1::bigint as id, 'slow'::text as name from pg_sleep(10)")]
    Task<Artist?> RunSlowQueryAsync(int _, CancellationToken cancellationToken = default);

    [RazQLQuery("insert into gmdb.transcriber(name) values (@Model.Bind()) returning id, name")]
    Task<Transcriber> CreateTranscriberAsync(string name, CancellationToken cancellationToken = default);

    [RazQLQuery("select id, name from gmdb.transcriber where name = @Model.Bind()")]
    Task<Transcriber?> FindTranscriberAsync(string name, CancellationToken cancellationToken = default);
}
