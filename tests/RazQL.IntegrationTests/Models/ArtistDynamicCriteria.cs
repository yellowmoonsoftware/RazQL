using RazQL.Binding;

namespace RazQL.IntegrationTests.Models;

public sealed record ArtistDynamicCriteria
{
    public ICollection<long>? Ids { get; init; }

    public string? NameContains { get; init; }

    public bool? RequireNameStartingWithA { get; init; }

    public ICollection<OrderSpec<ArtistOrder>>? OrderBy { get; init; }
}

public enum ArtistOrder
{
    Id,
    Name
}
