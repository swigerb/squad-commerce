using Microsoft.AspNetCore.SignalR;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Contracts.Interfaces;

namespace SquadCommerce.Api.Services;

/// <summary>
/// Broadcasts A2A handshake status over SignalR via <see cref="AgentHub"/>.
/// </summary>
public sealed class SignalRA2AStatusNotifier : IA2AStatusNotifier
{
    private readonly IHubContext<AgentHub> _hubContext;
    private readonly ILogger<SignalRA2AStatusNotifier> _logger;

    public SignalRA2AStatusNotifier(IHubContext<AgentHub> hubContext, ILogger<SignalRA2AStatusNotifier> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SendA2AHandshakeStatusAsync(
        string sessionId,
        string sourceAgent,
        string targetAgent,
        string status,
        string details,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.All.SendAsync("A2AHandshakeStatus", sessionId, sourceAgent, targetAgent, status, details, cancellationToken);
        _logger.LogDebug("A2AHandshakeStatus broadcast: Source={SourceAgent}, Target={TargetAgent}, Status={Status}, Session={SessionId}",
            sourceAgent, targetAgent, status, sessionId);
    }
}
