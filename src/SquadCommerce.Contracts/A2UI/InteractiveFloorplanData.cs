namespace SquadCommerce.Contracts.A2UI;

public sealed record InteractiveFloorplanData
{
    public required string StoreId { get; init; }
    public required string StoreName { get; init; }
    public required IReadOnlyList<FloorplanSection> Sections { get; init; }
    public required string FocusSection { get; init; }
    public required DateTimeOffset OpeningDate { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record FloorplanSection
{
    public required string SectionName { get; init; }
    public required int SquareFootage { get; init; }
    public required int ShelfCount { get; init; }
    public required double AvgHourlyTraffic { get; init; }
    public required string CurrentPlacement { get; init; }
    public required string SuggestedPlacement { get; init; }
    public required double TrafficIntensity { get; init; } // 0.0-1.0 for heatmap
    public required string OptimizationStatus { get; init; } // Optimal, NeedsAdjustment, Critical
}
