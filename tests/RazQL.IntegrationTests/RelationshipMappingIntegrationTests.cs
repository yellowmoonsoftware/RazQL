using Microsoft.Extensions.DependencyInjection;
using RazQL.IntegrationTests.Fixtures;
using RazQL.IntegrationTests.Mappers;

namespace RazQL.IntegrationTests;

[Collection(GmdbCollection.Name)]
public sealed class RelationshipMappingIntegrationTests(GmdbDatabaseFixture fixture)
{
    [Fact]
    public async Task FindWithCreditsAsync_AssemblesOneRootAndSharesArtistsAcrossRelationships()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var result = await mapper.FindWithCreditsAsync("AC/DC");

        Assert.NotNull(result);
        Assert.Equal("AC/DC", result.Name);
        Assert.NotNull(result.PrimaryArtist);
        Assert.Equal(result.Id, result.PrimaryArtist.Id);
        Assert.Equal([1L, 2L], result.Credits.Select(credit => credit.Id));
        Assert.All(result.Credits, credit => Assert.Same(result.PrimaryArtist, credit.Artist));
    }

    [Fact]
    public async Task FindWithCreditsAsync_RetainsRootWhenJoinedChildrenAreMissing()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var result = await mapper.FindWithCreditsAsync("Queen");

        Assert.NotNull(result);
        Assert.Equal("Queen", result.Name);
        Assert.Empty(result.Credits);
        Assert.NotNull(result.PrimaryArtist);
        Assert.Equal(result.Id, result.PrimaryArtist.Id);
    }

    [Fact]
    public async Task FindWithCreditsAsync_ReturnsNullForMissingRoot()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var result = await mapper.FindWithCreditsAsync("missing-association-test-artist");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindWithCreditsAsync_CreatesIndependentGraphsForSeparateExecutions()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();

        var first = await mapper.FindWithCreditsAsync("AC/DC");
        var second = await mapper.FindWithCreditsAsync("AC/DC");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
        Assert.NotSame(first.PrimaryArtist, second.PrimaryArtist);
        Assert.NotSame(first.Credits.First(), second.Credits.First());
    }
}
