using System.Transactions;
using Microsoft.Extensions.DependencyInjection;
using RazQL.IntegrationTests.Fixtures;
using RazQL.IntegrationTests.Mappers;

namespace RazQL.IntegrationTests;

[Collection(GmdbCollection.Name)]
public sealed class TransactionScopeIntegrationTests(GmdbDatabaseFixture fixture)
{
    [Fact]
    public async Task QueryExecutor_EnlistsConnectionInAmbientTransaction()
    {
        var mapper = fixture.Services.GetRequiredService<ITestArtistMapper>();
        var transcriberName = $"RazQL transaction {Guid.NewGuid():N}";

        using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            var inserted = await mapper.CreateTranscriberAsync(transcriberName);
            Assert.Equal(transcriberName, inserted.Name);
        }

        Assert.Null(await mapper.FindTranscriberAsync(transcriberName));
    }
}
