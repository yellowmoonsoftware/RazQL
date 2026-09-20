using Microsoft.Extensions.DependencyInjection;
using RazQL.Binding;
using RazQL.IntegrationTests.Fixtures;
using RazQL.IntegrationTests.Mappers;
using RazQL.IntegrationTests.Models;
using RazQL.Template;

namespace RazQL.IntegrationTests;

[Collection(GmdbCollection.Name)]
public sealed class TemplateLoaderIntegrationTests(GmdbDatabaseFixture fixture)
{
    [Fact]
    public async Task InlineQueryLoader_ExecutesQueryUsingBind()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var artist = await mapper.FindWithInlineQueryAsync("AC/DC");

        Assert.NotNull(artist);
        Assert.Equal("AC/DC", artist.Name);
    }

    [Fact]
    public async Task GeneratedMapper_RespectsNullableAndRequiredSingleResultSemantics()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();
        var missingName = $"missing-{Guid.NewGuid():N}";

        Assert.Null(await mapper.FindWithInlineQueryAsync(missingName));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mapper.RequireArtistAsync(missingName));
        Assert.Contains("returned no result", exception.Message);
    }

    [Fact]
    public async Task ResourceLoader_ExecutesTemplateUsingSelect()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();
        var criteria = new Models.ArtistSearchCriteria(["Aerosmith", "Queen"]);

        var artists = (await mapper.FindWithResourceAsync(criteria)).ToArray();

        Assert.Equal(["Aerosmith", "Queen"], artists.Select(artist => artist.Name));
    }

    [Fact]
    public async Task FileSystemLoader_ExecutesTemplateUsingBindAsArray()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var artists = (await mapper.FindWithFileSystemAsync(["AC/DC", "Rush"])).ToArray();

        Assert.Equal(["AC/DC", "Rush"], artists.Select(artist => artist.Name));
    }

    [Fact]
    public async Task FileSystemLoader_BindsNullCollectionAsEmptyArray()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var artists = await mapper.FindWithFileSystemAsync(null);

        Assert.Empty(artists);
    }

    [Fact]
    public async Task ResourceLoader_HonorsTemplateLocationAndNameOverrides()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var artist = await mapper.FindWithResourceOverridesAsync("Queen");

        Assert.NotNull(artist);
        Assert.Equal("Queen", artist.Name);
    }

    [Fact]
    public async Task ResourceLoader_ExecutesConditionalTemplateWithTransformAndOrdering()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();
        var criteria = new ArtistDynamicCriteria
        {
            Ids = [1, 2, 3],
            NameContains = "s",
            RequireNameStartingWithA = true,
            OrderBy = [new OrderSpec<ArtistOrder>(ArtistOrder.Id, OrderByDirection.Desc)]
        };

        var artists = (await mapper.SearchAsync(criteria)).ToArray();

        Assert.Equal(["Audioslave", "Aerosmith"], artists.Select(artist => artist.Name));
    }

    [Fact]
    public async Task CustomLoader_ExecutesTemplateRegisteredThroughDependencyInjection()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var artist = await mapper.FindWithCustomLoaderAsync("Rush");

        Assert.NotNull(artist);
        Assert.Equal("Rush", artist.Name);
    }

    [Fact]
    public async Task GeneratedMapper_PreloadsEveryTemplate()
    {
        var templateCache = fixture.Services.GetRequiredService<ITemplateCache>();
        var preloaders = fixture.Services.GetServices<IMapperTemplatePreloader>().ToArray();
        var preloadTasks = preloaders
            .SelectMany(preloader => preloader.PreloadTemplates(templateCache))
            .ToArray();

        Assert.NotEmpty(preloadTasks);
        await Task.WhenAll(preloadTasks);

        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();
        Assert.NotNull(await mapper.FindWithInlineQueryAsync("AC/DC"));
    }

    [Fact]
    public async Task GeneratedMapper_PropagatesCancellationToDatabaseCommand()
    {
        var templateCache = fixture.Services.GetRequiredService<ITemplateCache>();
        var preloader = Assert.Single(fixture.Services.GetServices<IMapperTemplatePreloader>());
        await Task.WhenAll(preloader.PreloadTemplates(templateCache));

        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mapper.RunSlowQueryAsync(0, cancellation.Token));
    }
}
