namespace SquadCommerce.Contracts.Models;

public record CompetitorPricing
{
    public required string Sku { get; init; }
    public required string CompetitorName { get; init; }
    public required decimal Price { get; init; }
    public required string Source { get; init; }
    public required bool Verified { get; init; }
    public required DateTimeOffset LastUpdated { get; init; }
    public string? ValidationNotes { get; init; }
}
