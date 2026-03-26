using Microsoft.AspNetCore.SignalR;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Contracts.Interfaces;

namespace SquadCommerce.Api.Services;

/// <summary>
/// Broadcasts agent thinking/active state over SignalR via <see cref="AgentHub"/>.
/// </summary>
public sealed class SignalRThinkingStateNotifier : IThinkingStateNotifier
{
    private readonly IHubContext<AgentHub> _hubContext;
    private readonly ILogger<SignalRThinkingStateNotifier> _logger;

    public SignalRThinkingStateNotifier(IHubContext<AgentHub> hubContext, ILogger<SignalRThinkingStateNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendThinkingStateAsync(string sessionId, string agentName, bool isThinking, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(sessionId).SendAsync("ThinkingState", sessionId, agentName, isThinking, cancellationToken);
        _logger.LogDebug("ThinkingState broadcast: Agent={AgentName}, IsThinking={IsThinking}, Session={SessionId}", agentName, isThinking, sessionId);
    }
}
