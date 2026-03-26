using Microsoft.AspNetCore.SignalR;
using SquadCommerce.Contracts;

namespace SquadCommerce.Api.Hubs;

/// <summary>
/// SignalR hub for background state updates.
/// Used for agent lifecycle events, urgency notifications, and push updates that don't fit the AG-UI request/response stream.
/// </summary>
public sealed class AgentHub : Hub
{
    private readonly ILogger<AgentHub> _logger;

    public AgentHub(ILogger<AgentHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Joins a session group for session-specific broadcasts.
    /// </summary>
    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogInformation("Client {ConnectionId} joined session {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Leaves a session group.
    /// </summary>
    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
        _logger.LogInformation("Client {ConnectionId} left session {SessionId}", Context.ConnectionId, sessionId);
    }

    /// <summary>
    /// Broadcasts agent status update to all clients in a session.
    /// </summary>
    public async Task SendStatusUpdate(string sessionId, string agentName, string status)
    {
        await Clients.Group(sessionId).SendAsync("StatusUpdate", agentName, status);
        _logger.LogDebug("Status update sent: Agent={AgentName}, Status={Status}, Session={SessionId}", agentName, status, sessionId);
    }

    /// <summary>
    /// Broadcasts urgency level change to all clients in a session.
    /// </summary>
    public async Task SendUrgencyUpdate(string sessionId, string level)
    {
        await Clients.Group(sessionId).SendAsync("UrgencyUpdate", level);
        _logger.LogDebug("Urgency update sent: Level={Level}, Session={SessionId}", level, sessionId);
    }

    /// <summary>
    /// Broadcasts A2UI component payload to all clients in a session.
    /// </summary>
    public async Task SendA2UIPayload(string sessionId, object payload)
    {
        await Clients.Group(sessionId).SendAsync("A2UIPayload", payload);
        _logger.LogDebug("A2UI payload sent to session {SessionId}", sessionId);
    }

    /// <summary>
    /// Broadcasts a notification message to all clients in a session.
    /// </summary>
    public async Task SendNotification(string sessionId, string message)
    {
        await Clients.Group(sessionId).SendAsync("Notification", message);
        _logger.LogDebug("Notification sent: Message={Message}, Session={SessionId}", message, sessionId);
    }

    /// <summary>
    /// Broadcasts agent thinking/active state to all clients in a session.
    /// </summary>
    public async Task SendThinkingState(string sessionId, string agentName, bool isThinking)
    {
        await Clients.Group(sessionId).SendAsync("ThinkingState", sessionId, agentName, isThinking);
        _logger.LogDebug("ThinkingState sent: Agent={AgentName}, IsThinking={IsThinking}, Session={SessionId}", agentName, isThinking, sessionId);
    }

    /// <summary>
    /// Broadcasts a chain of thought reasoning step to all connected clients.
    /// </summary>
    public async Task SendReasoningStep(ReasoningStep step)
    {
        await Clients.All.SendAsync("ReasoningStep", step);
        _logger.LogDebug("ReasoningStep sent: StepId={StepId}, Agent={AgentName}, Type={StepType}, Session={SessionId}",
            step.StepId, step.AgentName, step.StepType, step.SessionId);
    }
}
