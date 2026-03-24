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
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/agui")
        {
            Content = JsonContent.Create(new { message = userMessage })
        };

        HttpResponseMessage? response = null;
        Stream? stream = null;
        StreamReader? reader = null;

        try
        {
            response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            reader = new StreamReader(stream);

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(cancellationToken);

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

                            switch (type)
                            {
                                case "a2ui":
                                    if (root.TryGetProperty("payload", out var payloadProperty))
                                    {
                                        var payload = JsonSerializer.Deserialize<A2UIPayload>(
                                            payloadProperty.GetRawText(),
                                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                                        if (payload != null)
                                        {
                                            _logger.LogInformation("Received A2UI payload: {RenderAs}", payload.RenderAs);
                                            chunk = new StreamChunk(IsA2UI: true, Payload: payload);
                                        }
                                    }
                                    break;

                                case "text_delta":
                                case "text":
                                    if (root.TryGetProperty("text", out var textProperty))
                                    {
                                        var text = textProperty.GetString() ?? string.Empty;
                                        chunk = new StreamChunk(Text: text);
                                    }
                                    break;

                                case "status_update":
                                case "status":
                                    if (root.TryGetProperty("status", out var statusProperty))
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
            response?.Dispose();
        }
    }

    public record StreamChunk(
        bool IsA2UI = false,
        A2UIPayload? Payload = null,
        string Text = "",
        string Status = ""
    );
}
