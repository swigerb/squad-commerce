namespace SquadCommerce.Contracts.Interfaces;

/// <summary>
/// Notifies connected clients of A2A handshake status changes between agents.
/// Implemented via SignalR in the API layer.
/// </summary>
public interface IA2AStatusNotifier
{
    /// <summary>
    /// Broadcasts an A2A handshake status update.
    /// </summary>
    /// <param name="sessionId">The session context.</param>
    /// <param name="sourceAgent">The agent initiating the handshake.</param>
    /// <param name="targetAgent">The external agent being contacted.</param>
    /// <param name="status">One of: "negotiating", "connected", "completed", "failed".</param>
    /// <param name="details">Human-readable details about the handshake state.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SendA2AHandshakeStatusAsync(
        string sessionId,
        string sourceAgent,
        string targetAgent,
        string status,
        string details,
        CancellationToken cancellationToken = default);
}
