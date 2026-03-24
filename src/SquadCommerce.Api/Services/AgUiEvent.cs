using System.Text.Json;

namespace SquadCommerce.Api.Services;

/// <summary>
/// Represents an AG-UI event to be sent to the client via SSE.
/// </summary>
public sealed record AgUiEvent
{
    public required string Type { get; init; }
    public required object Data { get; init; }

    /// <summary>
    /// Converts the event to SSE format: "data: {json}\n\n"
    /// </summary>
    public string ToSseFormat()
    {
        var eventObject = new { type = Type, data = Data };
        var json = JsonSerializer.Serialize(eventObject);
        return $"data: {json}\n\n";
    }
}
