namespace SquadCommerce.Contracts.A2UI;

public sealed record SocialSentimentGraphData
{
    public required string Sku { get; init; }
    public required string ProductName { get; init; }
    public required IReadOnlyList<SentimentDataPoint> DataPoints { get; init; }
    public required string TrendDirection { get; init; } // "surging", "rising", "stable", "declining"
    public required decimal DemandMultiplier { get; init; }
    public required string Region { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record SentimentDataPoint
{
    public required string Platform { get; init; }
    public required double Score { get; init; }
    public required double Velocity { get; init; }
    public required DateTimeOffset MeasuredAt { get; init; }
}
