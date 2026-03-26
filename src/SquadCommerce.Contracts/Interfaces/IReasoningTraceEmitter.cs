namespace SquadCommerce.Contracts.Interfaces;

/// <summary>
/// Emits reasoning trace steps for chain of thought visualization.
/// Implementations broadcast steps to connected clients via SignalR.
/// </summary>
public interface IReasoningTraceEmitter
{
    Task EmitStepAsync(
        string sessionId,
        string agentName,
        ReasoningStepType stepType,
        string content,
        string? parentStepId = null,
        long durationMs = 0,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);
}
