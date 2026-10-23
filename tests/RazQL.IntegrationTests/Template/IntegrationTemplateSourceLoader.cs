using RazQL.Template;

namespace RazQL.IntegrationTests.Template;

public sealed class IntegrationTemplateSourceLoader : ITemplateSourceLoader
{
    public Task<string> LoadAsync(QueryDescriptor queryDescriptor, CancellationToken cancellationToken)
    {
        return Task.FromResult("""
            @inherits RazQL.RazQLModel<string, RazQL.IntegrationTests.Models.Artist>
            select id, name
            from gmdb.artist
            where name = @Bind(m => m)
            """);
    }
}
