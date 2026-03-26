using Microsoft.AspNetCore.SignalR;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Contracts;
using SquadCommerce.Contracts.Interfaces;

namespace SquadCommerce.Api.Services;

/// <summary>
/// Broadcasts reasoning trace steps to connected clients via the AgentHub SignalR hub.
/// </summary>
public sealed class SignalRReasoningTraceEmitter : IReasoningTraceEmitter
{
    private readonly IHubContext<AgentHub> _hubContext;
    private readonly ILogger<SignalRReasoningTraceEmitter> _logger;

    public SignalRReasoningTraceEmitter(IHubContext<AgentHub> hubContext, ILogger<SignalRReasoningTraceEmitter> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task EmitStepAsync(
        string sessionId,
        string agentName,
        ReasoningStepType stepType,
        string content,
        string? parentStepId = null,
        long durationMs = 0,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var stepId = metadata?.TryGetValue("StepId", out var requestedId) == true && !string.IsNullOrEmpty(requestedId)
            ? requestedId
            : Guid.NewGuid().ToString();

        var step = new ReasoningStep
        {
            StepId = stepId,
            SessionId = sessionId,
            AgentName = agentName,
            StepType = stepType,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow,
            DurationMs = durationMs,
            ParentStepId = parentStepId,
            Metadata = metadata
        };

        await _hubContext.Clients.All.SendAsync("ReasoningStep", step, cancellationToken);

        _logger.LogDebug(
            "ReasoningStep emitted: StepId={StepId}, Agent={AgentName}, Type={StepType}, Session={SessionId}",
            step.StepId, agentName, stepType, sessionId);
    }
}
