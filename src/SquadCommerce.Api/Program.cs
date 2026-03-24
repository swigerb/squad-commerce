using SquadCommerce.Api.Endpoints;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Api.Middleware;
using SquadCommerce.Api.Services;
using SquadCommerce.Agents.Registration;
using SquadCommerce.Mcp;
using SquadCommerce.A2A;
using SquadCommerce.Observability;
using System.Text;

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
        // Local development origins
        var localOrigins = new[] { "https://localhost:7001", "http://localhost:5001" };
        
        // Azure Container Apps: allow web service origin dynamically
        var azureWebOrigin = builder.Configuration["AllowedOrigins:Web"];
        var allowedOrigins = string.IsNullOrEmpty(azureWebOrigin) 
            ? localOrigins 
            : localOrigins.Concat(new[] { azureWebOrigin }).ToArray();

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// Initialize database (ensure created and seeded)
await app.UseSquadCommerceDatabaseAsync();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
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

// Endpoint groups
app.MapAgentEndpoints();
app.MapPricingEndpoints();

app.Run();
