namespace SquadCommerce.Contracts.A2UI;

public sealed record ReroutingMapData
{
    public required string Sku { get; init; }
    public required string ProductName { get; init; }
    public required IReadOnlyList<ReroutingRoute> Routes { get; init; }
    public required double OverallRiskScore { get; init; } // 0.0-1.0
    public required int DelayDays { get; init; }
    public required string DelayReason { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record ReroutingRoute
{
    public required string SourceStoreId { get; init; }
    public required string SourceStoreName { get; init; }
    public required string DestStoreId { get; init; }
    public required string DestStoreName { get; init; }
    public required int UnitsToTransfer { get; init; }
    public required string Priority { get; init; } // Critical, High, Medium, Low
    public required double DistanceMiles { get; init; }
    public required int EstimatedHours { get; init; }
}
