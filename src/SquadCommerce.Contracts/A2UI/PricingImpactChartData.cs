namespace SquadCommerce.Contracts.A2UI;

public record PricingImpactChartData
{
    public required string Sku { get; init; }
    public required decimal CurrentPrice { get; init; }
    public required decimal ProposedPrice { get; init; }
    public required IReadOnlyList<PriceScenario> Scenarios { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public record PriceScenario
{
    public required string ScenarioName { get; init; }
    public required decimal Price { get; init; }
    public required decimal EstimatedMargin { get; init; }
    public required decimal EstimatedRevenue { get; init; }
    public required int ProjectedUnitsSold { get; init; }
}
