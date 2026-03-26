using System.Runtime.CompilerServices;
using System.Text.Json;
using SquadCommerce.Contracts.A2UI;

namespace SquadCommerce.Web.Services;

public class AgUiStreamService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AgUiStreamService> _logger;

    public AgUiStreamService(HttpClient httpClient, ILogger<AgUiStreamService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async IAsyncEnumerable<StreamChunk> StreamAgUiAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Step 1: POST to chat bridge to get a sessionId
        var chatRequest = new HttpRequestMessage(HttpMethod.Post, "/api/agui/chat")
        {
            Content = JsonContent.Create(new { message = userMessage })
        };

        var chatResponse = await _httpClient.SendAsync(chatRequest, cancellationToken);

        if (!chatResponse.IsSuccessStatusCode)
        {
            var error = await chatResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Chat bridge returned {StatusCode}: {Error}", chatResponse.StatusCode, error);
            yield return new StreamChunk(Text: $"Error: {error}");
            yield break;
        }

        var responseJson = await chatResponse.Content.ReadAsStringAsync(cancellationToken);
        var responseDoc = JsonDocument.Parse(responseJson);
        var sessionId = responseDoc.RootElement.GetProperty("sessionId").GetString()!;

        _logger.LogInformation("Chat bridge created session {SessionId}, subscribing to stream...", sessionId);

        // Immediate feedback so the user knows something is happening
        yield return new StreamChunk(Status: "Connecting to agent stream...");

        // Step 2: GET the SSE stream with the sessionId
        // Brief delay lets the background orchestration write its first event
        await Task.Delay(1500, cancellationToken);

        var streamRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/agui?sessionId={sessionId}");
        HttpResponseMessage? streamResponse = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            streamResponse = await _httpClient.SendAsync(
                streamRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            streamResponse.EnsureSuccessStatusCode();

            stream = await streamResponse.Content.ReadAsStreamAsync(cancellationToken);
            reader = new StreamReader(stream);

            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null) break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // SSE format: "data: {json}"
                if (line.StartsWith("data: "))
                {
                    var jsonData = line["data: ".Length..];

                    if (jsonData == "[DONE]")
                    {
                        _logger.LogInformation("Stream completed with [DONE] marker");
                        yield break;
                    }

                    StreamChunk? chunk = null;

                    try
                    {
                        var jsonDoc = JsonDocument.Parse(jsonData);
                        var root = jsonDoc.RootElement;

                        if (root.TryGetProperty("type", out var typeProperty))
                        {
                            var type = typeProperty.GetString();

                            // Server wraps SSE events as {"type":"...","data":{...}}
                            // Read content properties from nested "data", fallback to root for backward compat
                            JsonElement dataElement;
                            if (root.TryGetProperty("data", out var dataEl))
                                dataElement = dataEl;
                            else
                                dataElement = root;

                            switch (type)
                            {
                                case "a2ui":
                                case "a2ui_payload":
                                    // a2ui_payload: data IS the payload directly
                                    var payloadJson = dataElement.GetRawText();
                                    var payload = JsonSerializer.Deserialize<A2UIPayload>(
                                        payloadJson,
                                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                    if (payload != null)
                                    {
                                        _logger.LogInformation("Received A2UI payload: {RenderAs}", payload.RenderAs);
                                        chunk = new StreamChunk(IsA2UI: true, Payload: payload);
                                    }
                                    break;

                                case "text_delta":
                                case "text":
                                    if (dataElement.TryGetProperty("text", out var textProperty))
                                    {
                                        var text = textProperty.GetString() ?? string.Empty;
                                        chunk = new StreamChunk(Text: text);
                                    }
                                    break;

                                case "status_update":
                                case "status":
                                    if (dataElement.TryGetProperty("status", out var statusProperty))
                                    {
                                        var status = statusProperty.GetString() ?? string.Empty;
                                        _logger.LogInformation("Status update: {Status}", status);
                                        chunk = new StreamChunk(Status: status);
                                    }
                                    break;

                                case "tool_call":
                                    // Future: Handle tool call visualization
                                    _logger.LogDebug("Received tool_call event (not yet implemented)");
                                    break;

                                case "done":
                                    _logger.LogInformation("Stream completed with done event");
                                    yield break;

                                default:
                                    _logger.LogWarning("Unknown event type: {Type}", type);
                                    break;
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse SSE JSON: {Data}", jsonData);
                        continue;
                    }

                    if (chunk != null)
                    {
                        yield return chunk;
                    }
                }
            }

            _logger.LogInformation("Stream ended normally");
        }
        finally
        {
            reader?.Dispose();
            stream?.Dispose();
            streamResponse?.Dispose();
        }
    }

    public record StreamChunk(
        bool IsA2UI = false,
        A2UIPayload? Payload = null,
        string Text = "",
        string Status = ""
    );
}
