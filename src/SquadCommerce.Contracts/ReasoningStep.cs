namespace SquadCommerce.Contracts;

/// <summary>
/// Represents a single step in an agent's chain of thought reasoning trace.
/// Used to visualize agent decision-making in the command center UI.
/// </summary>
public sealed record ReasoningStep
{
    public required string StepId { get; init; }
    public required string SessionId { get; init; }
    public required string AgentName { get; init; }
    public required ReasoningStepType StepType { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public long DurationMs { get; init; }
    public string? ParentStepId { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

public enum ReasoningStepType
{
    Thinking,
    ToolCall,
    A2AHandshake,
    Observation,
    Decision,
    Error
}
