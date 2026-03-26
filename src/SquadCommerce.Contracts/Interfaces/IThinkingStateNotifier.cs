namespace SquadCommerce.Contracts.Interfaces;

/// <summary>
/// Notifies connected clients when an agent enters or exits a thinking/active state.
/// Implemented via SignalR in the API layer.
/// </summary>
public interface IThinkingStateNotifier
{
    Task SendThinkingStateAsync(string sessionId, string agentName, bool isThinking, CancellationToken cancellationToken = default);
}
