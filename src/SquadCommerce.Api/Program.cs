using SquadCommerce.Api.Endpoints;
using SquadCommerce.Api.Hubs;
using SquadCommerce.Api.Middleware;
using SquadCommerce.Api.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register AG-UI stream writer
builder.Services.AddSingleton<IAgUiStreamWriter, AgUiStreamWriter>();

// SignalR for background state updates
builder.Services.AddSignalR();

// CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7001", "http://localhost:5001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseHttpsRedirection();
app.UseCors();

// Entra ID scope validation middleware (demo mode by default)
app.UseMiddleware<EntraIdScopeMiddleware>();

// SignalR Hub for background state updates
app.MapHub<AgentHub>("/hubs/agent");

// AG-UI SSE streaming endpoint
app.MapGet("/api/agui", async (string sessionId, IAgUiStreamWriter streamWriter, HttpContext context, CancellationToken cancellationToken) =>
{
    context.Response.Headers["Content-Type"] = "text/event-stream";
    context.Response.Headers["Cache-Control"] = "no-cache";
    context.Response.Headers["Connection"] = "keep-alive";

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
