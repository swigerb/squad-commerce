namespace SquadCommerce.Api.Services;

/// <summary>
/// Provides an interface for agents to push AG-UI events to the streaming endpoint.
/// </summary>
public interface IAgUiStreamWriter
{
    /// <summary>
    /// Writes a text delta event to the AG-UI stream.
    /// </summary>
    Task WriteTextDeltaAsync(string sessionId, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a tool call notification event to the AG-UI stream.
    /// </summary>
    Task WriteToolCallAsync(string sessionId, string toolName, object parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a status update event to the AG-UI stream.
    /// </summary>
    Task WriteStatusUpdateAsync(string sessionId, string status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes an A2UI payload event to the AG-UI stream.
    /// </summary>
    Task WriteA2UIPayloadAsync(string sessionId, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a done event to the AG-UI stream, signaling completion.
    /// </summary>
    Task WriteDoneAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to AG-UI events for a specific session.
    /// </summary>
    IAsyncEnumerable<AgUiEvent> SubscribeAsync(string sessionId, CancellationToken cancellationToken = default);
}
