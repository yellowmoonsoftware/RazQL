namespace RazQL.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class GmdbCollection : ICollectionFixture<GmdbDatabaseFixture>
{
    public const string Name = "GMDB";
}
