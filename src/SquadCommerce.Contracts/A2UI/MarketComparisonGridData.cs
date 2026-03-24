namespace SquadCommerce.Contracts.A2UI;

public record MarketComparisonGridData
{
    public required string Sku { get; init; }
    public required string ProductName { get; init; }
    public required IReadOnlyList<CompetitorPrice> Competitors { get; init; }
    public required decimal OurPrice { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public record CompetitorPrice
{
    public required string CompetitorName { get; init; }
    public required decimal Price { get; init; }
    public required string Source { get; init; }
    public required bool Verified { get; init; }
    public required DateTimeOffset LastUpdated { get; init; }
}
