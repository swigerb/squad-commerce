namespace SquadCommerce.Agents;

/// <summary>
/// Common interface for all domain agents in Squad-Commerce.
/// Agents execute specific tasks and return structured results with A2UI payloads.
/// </summary>
public interface IDomainAgent
{
    /// <summary>
    /// Gets the agent's name for telemetry and logging.
    /// </summary>
    string AgentName { get; }
}

/// <summary>
/// Result returned by agent execution with both text and A2UI payload.
/// </summary>
public sealed record AgentResult
{
    /// <summary>
    /// Plain text summary suitable for logging and non-UI contexts.
    /// </summary>
    public required string TextSummary { get; init; }

    /// <summary>
    /// Structured A2UI payload for rendering in Blazor components.
    /// Null if agent doesn't produce visual data (e.g., write-only operations).
    /// </summary>
    public object? A2UIPayload { get; init; }

    /// <summary>
    /// Whether the agent execution succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if Success is false.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Timestamp of execution.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }
}
