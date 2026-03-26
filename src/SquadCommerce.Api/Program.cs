using SquadCommerce.Api.Endpoints;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Api.Middleware;
using SquadCommerce.Api.Services;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Agents.Registration;
using SquadCommerce.Mcp;
using SquadCommerce.A2A;
using SquadCommerce.Observability;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults (includes OpenTelemetry tracing and metrics)
builder.AddServiceDefaults();

// Register Squad-Commerce observability
builder.AddSquadCommerceHealthChecks();

// Register Squad-Commerce metrics singleton
builder.Services.AddSingleton<SquadCommerceMetrics>();

// Register MCP infrastructure (repositories + tools)
builder.Services.AddSquadCommerceMcp();

// Register A2A infrastructure (client + server)
builder.Services.AddSquadCommerceA2A();

// Register MAF agents (orchestrator + domain agents + policies)
builder.Services.AddSquadCommerceAgents();

// Register AG-UI stream writer
builder.Services.AddSingleton<IAgUiStreamWriter, AgUiStreamWriter>();

// SignalR for background state updates
builder.Services.AddSignalR();

// CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Allow any localhost origin — Aspire assigns ports dynamically
            policy.SetIsOriginAllowed(origin =>
                new Uri(origin).Host == "localhost")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
        else
        {
            // Production: explicit origins only
            var allowedOrigins = new List<string>();
            var azureWebOrigin = builder.Configuration["AllowedOrigins:Web"];
            if (!string.IsNullOrEmpty(azureWebOrigin))
                allowedOrigins.Add(azureWebOrigin);

            policy.WithOrigins(allowedOrigins.ToArray())
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

var app = builder.Build();

// Initialize database (ensure created and seeded)
await app.UseSquadCommerceDatabaseAsync();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();

// Entra ID scope validation middleware (demo mode by default)
app.UseMiddleware<EntraIdScopeMiddleware>();

// SignalR Hub for background state updates
app.MapHub<AgentHub>("/hubs/agent");

// AG-UI SSE streaming endpoint
app.MapGet("/api/agui", async (string sessionId, IAgUiStreamWriter streamWriter, SquadCommerceMetrics metrics, HttpContext context, CancellationToken cancellationToken) =>
{
    context.Response.Headers["Content-Type"] = "text/event-stream";
    context.Response.Headers["Cache-Control"] = "no-cache";
    context.Response.Headers["Connection"] = "keep-alive";

    using var activity = metrics.StartAgUiSpan(sessionId, "stream_connection");

    try
    {
        await foreach (var evt in streamWriter.SubscribeAsync(sessionId, cancellationToken))
        {
            var sseData = evt.ToSseFormat();
            await context.Response.WriteAsync(sseData, Encoding.UTF8, cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }
    catch (OperationCanceledException)
    {
        // Client disconnected - expected behavior
    }
})
.WithName("GetAgUiStream")
.WithSummary("AG-UI Server-Sent Events stream for agent communication")
.WithTags("AG-UI");

// AG-UI Chat Bridge — accepts free-text, extracts intent, launches orchestration
app.MapPost("/api/agui/chat", async (ChatRequest chatRequest, IAgUiStreamWriter streamWriter, IServiceProvider serviceProvider, SquadCommerceMetrics metrics, ILogger<ChatRequest> logger, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(chatRequest.Message))
        return Results.BadRequest("Message is required.");

    var sessionId = Guid.NewGuid().ToString();
    var message = chatRequest.Message.Trim();

    // Extract intent from free-text using simple pattern matching
    var skuMatch = Regex.Match(message, @"SKU-\d+", RegexOptions.IgnoreCase);
    var priceMatch = Regex.Match(message, @"\$?([\d]+\.?\d*)");

    var sku = skuMatch.Success ? skuMatch.Value.ToUpper() : "SKU-100";
    var competitorPrice = priceMatch.Success && decimal.TryParse(priceMatch.Groups[1].Value, out var price) ? price : 24.99m;
    var competitorName = "MegaMart";

    var competitors = new[] { "walmart", "amazon", "target", "bestbuy", "costco", "megamart" };
    foreach (var c in competitors)
    {
        if (message.Contains(c, StringComparison.OrdinalIgnoreCase))
        {
            competitorName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(c);
            break;
        }
    }

    logger.LogInformation("Chat bridge: SessionId={SessionId}, Message={Message}, Extracted: Sku={Sku}, Competitor={Competitor}, Price={Price}",
        sessionId, message, sku, competitorName, competitorPrice);

    // Launch orchestration in background (reuse existing TriggerAnalysis pattern)
    _ = Task.Run(async () =>
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var scope = serviceProvider.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<ChiefSoftwareArchitectAgent>();

            await streamWriter.WriteStatusUpdateAsync(sessionId, $"Analyzing: {sku} vs {competitorName} at ${competitorPrice:F2}...", cancellationToken);

            var result = await orchestrator.ProcessCompetitorPriceDropAsync(sku, competitorPrice, cancellationToken);

            if (!result.Success)
            {
                await streamWriter.WriteTextDeltaAsync(sessionId, $"Analysis failed: {result.ErrorMessage}", cancellationToken);
                await streamWriter.WriteDoneAsync(sessionId, cancellationToken);

                stopwatch.Stop();
                metrics.RecordAgentInvocation("ChiefSoftwareArchitect", stopwatch.Elapsed.TotalMilliseconds, false);
                return;
            }

            foreach (var agentResult in result.AgentResults)
            {
                if (agentResult.A2UIPayload != null)
                    await streamWriter.WriteA2UIPayloadAsync(sessionId, agentResult.A2UIPayload, cancellationToken);
            }

            await streamWriter.WriteTextDeltaAsync(sessionId, result.ExecutiveSummary, cancellationToken);
            await streamWriter.WriteDoneAsync(sessionId, cancellationToken);

            stopwatch.Stop();
            logger.LogInformation("Chat bridge analysis completed: SessionId={SessionId}, Duration={DurationMs}ms",
                sessionId, stopwatch.Elapsed.TotalMilliseconds);
            metrics.RecordAgentInvocation("ChiefSoftwareArchitect", stopwatch.Elapsed.TotalMilliseconds, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Chat bridge error for session {SessionId}", sessionId);
            try
            {
                await streamWriter.WriteTextDeltaAsync(sessionId, $"Error: {ex.Message}", cancellationToken);
                await streamWriter.WriteDoneAsync(sessionId, cancellationToken);
            }
            catch { }

            stopwatch.Stop();
            metrics.RecordAgentInvocation("ChiefSoftwareArchitect", stopwatch.Elapsed.TotalMilliseconds, false);
        }
    }, cancellationToken);

    return Results.Accepted($"/api/agui?sessionId={sessionId}", new { sessionId, streamUrl = $"/api/agui?sessionId={sessionId}" });
})
.WithName("ChatBridge")
.WithSummary("Accept freeform chat input, interpret intent, and start orchestration")
.WithTags("AG-UI");

// Endpoint groups
app.MapAgentEndpoints();
app.MapPricingEndpoints();

app.Run();

/// <summary>
/// Request model for the AG-UI chat bridge endpoint.
/// </summary>
public sealed record ChatRequest
{
    public required string Message { get; init; }
}
