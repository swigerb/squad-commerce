using System.Collections.Concurrent;
using System.Threading.Channels;
using SquadCommerce.Observability;

namespace SquadCommerce.Api.Services;

/// <summary>
/// In-memory implementation of AG-UI stream writer using channels for pub/sub.
/// </summary>
public sealed class AgUiStreamWriter : IAgUiStreamWriter
{
    private readonly ConcurrentDictionary<string, Channel<AgUiEvent>> _sessions = new();
    private readonly ILogger<AgUiStreamWriter> _logger;
    private readonly SquadCommerceMetrics _metrics;

    public AgUiStreamWriter(ILogger<AgUiStreamWriter> logger, SquadCommerceMetrics metrics)
    {
        _logger = logger;
        _metrics = metrics;
    }

    public async Task WriteTextDeltaAsync(string sessionId, string text, CancellationToken cancellationToken = default)
    {
        var evt = new AgUiEvent { Type = "text_delta", Data = new { text } };
        await WriteEventAsync(sessionId, evt, cancellationToken);
    }

    public async Task WriteToolCallAsync(string sessionId, string toolName, object parameters, CancellationToken cancellationToken = default)
    {
        var evt = new AgUiEvent { Type = "tool_call", Data = new { tool = toolName, parameters } };
        await WriteEventAsync(sessionId, evt, cancellationToken);
    }

    public async Task WriteStatusUpdateAsync(string sessionId, string status, CancellationToken cancellationToken = default)
    {
        var evt = new AgUiEvent { Type = "status_update", Data = new { status } };
        await WriteEventAsync(sessionId, evt, cancellationToken);
    }

    public async Task WriteA2UIPayloadAsync(string sessionId, object payload, CancellationToken cancellationToken = default)
    {
        using var activity = _metrics.StartAgUiSpan(sessionId, "a2ui_payload");
        
        var evt = new AgUiEvent { Type = "a2ui_payload", Data = payload };
        await WriteEventAsync(sessionId, evt, cancellationToken);

        // Extract component type from payload for metrics
        var componentType = ExtractComponentType(payload);
        _metrics.RecordA2UIPayload(componentType, sessionId);
        
        _logger.LogInformation("A2UI payload emitted: SessionId={SessionId}, ComponentType={ComponentType}", 
            sessionId, componentType);
    }

    public async Task WriteDoneAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var evt = new AgUiEvent { Type = "done", Data = new { completed = true } };
        await WriteEventAsync(sessionId, evt, cancellationToken);
    }

    public async IAsyncEnumerable<AgUiEvent> SubscribeAsync(string sessionId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var channel = _sessions.GetOrAdd(sessionId, _ => Channel.CreateUnbounded<AgUiEvent>());
        _logger.LogInformation("Client subscribed to AG-UI stream for session {SessionId}", sessionId);

        await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return evt;
        }
    }

    private async Task WriteEventAsync(string sessionId, AgUiEvent evt, CancellationToken cancellationToken)
    {
        var channel = _sessions.GetOrAdd(sessionId, _ => Channel.CreateUnbounded<AgUiEvent>());
        await channel.Writer.WriteAsync(evt, cancellationToken);
        _logger.LogDebug("AG-UI event written: Session={SessionId}, Type={Type}", sessionId, evt.Type);
    }

    private static string ExtractComponentType(object payload)
    {
        // Try to extract RenderAs property for A2UI payloads
        var payloadType = payload.GetType();
        var renderAsProp = payloadType.GetProperty("RenderAs");
        if (renderAsProp != null)
        {
            var value = renderAsProp.GetValue(payload);
            if (value is string componentType)
            {
                return componentType;
            }
        }
        return "unknown";
    }
}
