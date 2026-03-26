namespace SquadCommerce.Contracts.A2UI;

/// <summary>
/// Represents a synthesized insight card emitted by the orchestrator.
/// Rendered as a visually rich summary card in the chat stream and dashboard.
/// </summary>
public sealed record InsightCardData
{
    /// <summary>
    /// Card title displayed at the top (e.g., "Margin Impact").
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Large hero metric displayed prominently (e.g., "-12.5%", "$4,200", "3 stores").
    /// </summary>
    public required string KeyMetric { get; init; }

    /// <summary>
    /// Label beneath the key metric (e.g., "margin change", "revenue delta").
    /// </summary>
    public required string MetricLabel { get; init; }

    /// <summary>
    /// Trend direction: "up", "down", or "neutral".
    /// Controls the trend arrow indicator (▲/▼/─).
    /// </summary>
    public required string TrendDirection { get; init; }

    /// <summary>
    /// Narrative summary paragraph explaining the insight.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Optional call-to-action button label.
    /// </summary>
    public string? ActionLabel { get; init; }

    /// <summary>
    /// Severity level controlling accent color: "info", "warning", "critical", "success".
    /// </summary>
    public string? Severity { get; init; }
}
