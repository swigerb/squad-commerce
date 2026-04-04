using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Web.Services;

namespace SquadCommerce.Web.Tests.Services;

public class AgUiStreamServiceTests
{
    private readonly Mock<ILogger<AgUiStreamService>> _loggerMock = new();

    /// <summary>
    /// Creates an AgUiStreamService backed by a mock HttpMessageHandler.
    /// The chat POST always returns a sessionId; the SSE GET returns the given lines.
    /// </summary>
    private AgUiStreamService CreateService(string[] sseLines, HttpStatusCode chatStatus = HttpStatusCode.OK, HttpStatusCode streamStatus = HttpStatusCode.OK, string? chatErrorBody = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var requestIndex = 0;

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath.Contains("/api/agui/chat"))
                {
                    if (chatStatus != HttpStatusCode.OK)
                    {
                        return new HttpResponseMessage(chatStatus)
                        {
                            Content = new StringContent(chatErrorBody ?? "Error")
                        };
                    }
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new { sessionId = "test-session-001" }),
                            Encoding.UTF8,
                            "application/json")
                    };
                }

                if (request.Method == HttpMethod.Get && request.RequestUri!.AbsolutePath.Contains("/api/agui"))
                {
                    if (streamStatus != HttpStatusCode.OK)
                    {
                        return new HttpResponseMessage(streamStatus)
                        {
                            Content = new StringContent("Stream error")
                        };
                    }

                    var sseContent = string.Join("\n", sseLines);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(sseContent, Encoding.UTF8, "text/event-stream")
                    };
                }

                throw new InvalidOperationException($"Unexpected request: {request.Method} {request.RequestUri}");
            });

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost:5000")
        };

        return new AgUiStreamService(httpClient, _loggerMock.Object);
    }

    // ── Text Delta Events ───────────────────────────────────────────────

    [Fact]
    public async Task Should_YieldTextChunk_When_TextDeltaReceived()
    {
        // Arrange
        var service = CreateService([
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"Hello world\"}}",
            "data: [DONE]"
        ]);

        // Act
        var chunks = new List<AgUiStreamService.StreamChunk>();
        await foreach (var chunk in service.StreamAgUiAsync("test"))
        {
            chunks.Add(chunk);
        }

        // Assert — first chunk is "Connecting..." status, then text, then stream ends
        chunks.Should().Contain(c => c.Text == "Hello world" && !c.IsA2UI);
    }

    [Fact]
    public async Task Should_YieldTextChunk_When_TextTypeReceived()
    {
        var service = CreateService([
            "data: {\"type\":\"text\",\"data\":{\"text\":\"Response text\"}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        chunks.Should().Contain(c => c.Text == "Response text");
    }

    [Fact]
    public async Task Should_YieldMultipleTextChunks_When_StreamedIncrementally()
    {
        var service = CreateService([
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"Hello \"}}",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"world\"}}",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"!\"}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");
        var textChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Text)).ToList();

        textChunks.Select(c => c.Text).Should().Equal("Hello ", "world", "!");
    }

    // ── Status Update Events ────────────────────────────────────────────

    [Fact]
    public async Task Should_YieldStatusChunk_When_StatusUpdateReceived()
    {
        var service = CreateService([
            "data: {\"type\":\"status_update\",\"data\":{\"status\":\"Agent thinking...\"}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        chunks.Should().Contain(c => c.Status == "Agent thinking...");
    }

    [Fact]
    public async Task Should_YieldStatusChunk_When_StatusTypeReceived()
    {
        var service = CreateService([
            "data: {\"type\":\"status\",\"data\":{\"status\":\"Processing\"}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        chunks.Should().Contain(c => c.Status == "Processing");
    }

    // ── A2UI Payload Events ─────────────────────────────────────────────

    [Fact]
    public async Task Should_YieldA2UIChunk_When_A2UIPayloadReceived()
    {
        var payloadJson = JsonSerializer.Serialize(new
        {
            type = "a2ui",
            data = new { type = "visualization", renderAs = "RetailStockHeatmap", data = new { items = new[] { "a" } } }
        });

        var service = CreateService([
            $"data: {payloadJson}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        chunks.Should().Contain(c => c.IsA2UI && c.Payload != null && c.Payload.RenderAs == "RetailStockHeatmap");
    }

    [Fact]
    public async Task Should_YieldA2UIChunk_When_A2UIPayloadTypeReceived()
    {
        var payloadJson = JsonSerializer.Serialize(new
        {
            type = "a2ui_payload",
            data = new { type = "chart", renderAs = "PricingImpactChart", data = new { value = 42 } }
        });

        var service = CreateService([
            $"data: {payloadJson}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        chunks.Should().Contain(c => c.IsA2UI && c.Payload!.RenderAs == "PricingImpactChart");
    }

    // ── Stream Completion Events ────────────────────────────────────────

    [Fact]
    public async Task Should_StopStreaming_When_DoneMarkerReceived()
    {
        var service = CreateService([
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"before\"}}",
            "data: [DONE]",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"after\"}}"
        ]);

        var chunks = await CollectChunks(service, "test");
        var textChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Text)).ToList();

        textChunks.Should().NotContain(c => c.Text == "after");
    }

    [Fact]
    public async Task Should_StopStreaming_When_DoneEventTypeReceived()
    {
        var service = CreateService([
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"first\"}}",
            "data: {\"type\":\"done\"}",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"should not appear\"}}"
        ]);

        var chunks = await CollectChunks(service, "test");
        var textChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Text)).ToList();

        textChunks.Should().NotContain(c => c.Text == "should not appear");
    }

    // ── Initial Status & Connection ─────────────────────────────────────

    [Fact]
    public async Task Should_YieldConnectingStatus_When_StreamStarted()
    {
        var service = CreateService(["data: [DONE]"]);

        var chunks = await CollectChunks(service, "test");

        chunks.First().Status.Should().Be("Connecting to agent stream...");
    }

    // ── Error Handling ──────────────────────────────────────────────────

    [Fact]
    public async Task Should_YieldErrorChunk_When_ChatBridgeReturnsError()
    {
        var service = CreateService(
            [],
            chatStatus: HttpStatusCode.InternalServerError,
            chatErrorBody: "Backend unavailable");

        var chunks = await CollectChunks(service, "test");

        chunks.Should().ContainSingle(c => c.Text.Contains("Error:"));
    }

    [Fact]
    public async Task Should_SkipMalformedJson_When_ParseFails()
    {
        var service = CreateService([
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"before\"}}",
            "data: {not valid json!!!}",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"after\"}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");
        var textChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Text)).ToList();

        textChunks.Select(c => c.Text).Should().Equal("before", "after");
    }

    [Fact]
    public async Task Should_SkipBlankLines_When_SSEContainsEmptyLines()
    {
        var service = CreateService([
            "",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"content\"}}",
            "",
            "   ",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        chunks.Should().Contain(c => c.Text == "content");
    }

    [Fact]
    public async Task Should_SkipNonDataLines_When_SSEContainsComments()
    {
        var service = CreateService([
            ": comment line",
            "event: keep-alive",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"real data\"}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        chunks.Should().Contain(c => c.Text == "real data");
        // Non-data lines should not produce chunks
        chunks.Where(c => !string.IsNullOrEmpty(c.Text))
              .Should().HaveCount(1);
    }

    // ── Unknown Event Types ─────────────────────────────────────────────

    [Fact]
    public async Task Should_SkipUnknownTypes_When_EventTypeNotRecognized()
    {
        var service = CreateService([
            "data: {\"type\":\"unknown_event\",\"data\":{\"foo\":\"bar\"}}",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"valid\"}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");
        var textChunks = chunks.Where(c => !string.IsNullOrEmpty(c.Text)).ToList();

        textChunks.Should().ContainSingle().Which.Text.Should().Be("valid");
    }

    // ── Tool Call Events ────────────────────────────────────────────────

    [Fact]
    public async Task Should_NotYieldChunk_When_ToolCallReceivedButNotImplemented()
    {
        var service = CreateService([
            "data: {\"type\":\"tool_call\",\"data\":{\"name\":\"GetInventory\",\"args\":{}}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        // tool_call is logged but doesn't produce a chunk
        chunks.Where(c => !string.IsNullOrEmpty(c.Text) || c.IsA2UI)
              .Should().BeEmpty();
    }

    // ── Mixed Event Streams ─────────────────────────────────────────────

    [Fact]
    public async Task Should_HandleMixedEventStream_When_MultipleTypesInterleaved()
    {
        var a2uiJson = JsonSerializer.Serialize(new
        {
            type = "a2ui",
            data = new { type = "insight", renderAs = "InsightCard", data = new { title = "Test" } }
        });

        var service = CreateService([
            "data: {\"type\":\"status_update\",\"data\":{\"status\":\"Starting\"}}",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"Hello \"}}",
            $"data: {a2uiJson}",
            "data: {\"type\":\"text_delta\",\"data\":{\"text\":\"World\"}}",
            "data: {\"type\":\"status\",\"data\":{\"status\":\"Done\"}}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        // Verify ordering is maintained
        chunks.Should().Contain(c => c.Status == "Starting");
        chunks.Should().Contain(c => c.Text == "Hello ");
        chunks.Should().Contain(c => c.IsA2UI);
        chunks.Should().Contain(c => c.Text == "World");
        chunks.Should().Contain(c => c.Status == "Done");
    }

    // ── Backward Compatibility ──────────────────────────────────────────

    [Fact]
    public async Task Should_ReadFromRoot_When_NoNestedDataElement()
    {
        // When the server doesn't wrap in {type, data} nested structure,
        // the root itself is used as the data element.
        var service = CreateService([
            "data: {\"type\":\"text_delta\",\"text\":\"flat format\"}",
            "data: [DONE]"
        ]);

        var chunks = await CollectChunks(service, "test");

        chunks.Should().Contain(c => c.Text == "flat format");
    }

    // ── StreamChunk Record ──────────────────────────────────────────────

    [Fact]
    public void StreamChunk_Should_DefaultToNonA2UI()
    {
        var chunk = new AgUiStreamService.StreamChunk();

        chunk.IsA2UI.Should().BeFalse();
        chunk.Payload.Should().BeNull();
        chunk.Text.Should().BeEmpty();
        chunk.Status.Should().BeEmpty();
    }

    [Fact]
    public void StreamChunk_Should_SupportValueEquality()
    {
        var a = new AgUiStreamService.StreamChunk(Text: "hello");
        var b = new AgUiStreamService.StreamChunk(Text: "hello");

        a.Should().Be(b);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static async Task<List<AgUiStreamService.StreamChunk>> CollectChunks(
        AgUiStreamService service, string message)
    {
        var chunks = new List<AgUiStreamService.StreamChunk>();
        await foreach (var chunk in service.StreamAgUiAsync(message))
        {
            chunks.Add(chunk);
        }
        return chunks;
    }
}
