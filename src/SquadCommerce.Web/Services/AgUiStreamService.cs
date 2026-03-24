using System.Runtime.CompilerServices;
using System.Text.Json;
using SquadCommerce.Contracts.A2UI;

namespace SquadCommerce.Web.Services;

public class AgUiStreamService
{
    private readonly HttpClient _httpClient;

    public AgUiStreamService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async IAsyncEnumerable<StreamChunk> StreamAgUiAsync(
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/agui")
        {
            Content = JsonContent.Create(new { message = userMessage })
        };

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.StartsWith("data: "))
            {
                var jsonData = line["data: ".Length..];

                if (jsonData == "[DONE]")
                {
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

                        if (type == "a2ui" && root.TryGetProperty("payload", out var payloadProperty))
                        {
                            var payload = JsonSerializer.Deserialize<A2UIPayload>(
                                payloadProperty.GetRawText(),
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            chunk = new StreamChunk(IsA2UI: true, Payload: payload);
                        }
                        else if (type == "text" && root.TryGetProperty("text", out var textProperty))
                        {
                            chunk = new StreamChunk(Text: textProperty.GetString() ?? string.Empty);
                        }
                        else if (type == "status" && root.TryGetProperty("status", out var statusProperty))
                        {
                            chunk = new StreamChunk(Status: statusProperty.GetString() ?? string.Empty);
                        }
                    }
                }
                catch (JsonException)
                {
                    continue;
                }

                if (chunk != null)
                {
                    yield return chunk;
                }
            }
        }
    }

    public record StreamChunk(
        bool IsA2UI = false,
        A2UIPayload? Payload = null,
        string Text = "",
        string Status = ""
    );
}
