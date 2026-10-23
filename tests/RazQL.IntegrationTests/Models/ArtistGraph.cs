namespace RazQL.IntegrationTests.Models;

public sealed class ArtistGraph
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public ICollection<ArtistCredit>? Credits { get; set; }
    public Artist? PrimaryArtist { get; set; }
    public Artist? SecondaryArtist { get; set; }
}

public sealed class ArtistCredit
{
    public long Id { get; set; }
    public required Artist Artist { get; set; }
}
