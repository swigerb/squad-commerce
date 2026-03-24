using Microsoft.AspNetCore.Http.HttpResults;
using SquadCommerce.Api.Services;
using SquadCommerce.Observability;
using System.Diagnostics;

namespace SquadCommerce.Api.Endpoints;

public static class AgentEndpoints
{
    public static IEndpointRouteBuilder MapAgentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agents")
            .WithTags("Agents");

        group.MapGet("/", GetAgents)
            .WithName("GetAgents")
            .WithSummary("List registered agents and their policies");

        group.MapGet("/{name}/status", GetAgentStatus)
            .WithName("GetAgentStatus")
            .WithSummary("Get agent status");

        group.MapPost("/analyze", TriggerAnalysis)
            .WithName("TriggerAnalysis")
            .WithSummary("Trigger competitor price drop analysis scenario");
        
        return app;
    }

    /// <summary>
    /// Lists all registered agents and their policy configuration.
    /// </summary>
    private static Ok<AgentListResponse> GetAgents()
    {
        // Mock data for now - will be replaced with AgentPolicyRegistry
        var agents = new[]
        {
            new AgentInfo
            {
                Name = "ChiefSoftwareArchitect",
                Role = "Orchestrator",
                EntraIdScope = "SquadCommerce.Orchestrate",
                AllowedTools = Array.Empty<string>(),
                PreferredProtocol = "AGUI"
            },
            new AgentInfo
            {
                Name = "InventoryAgent",
                Role = "Domain",
                EntraIdScope = "SquadCommerce.Inventory.Read",
                AllowedTools = new[] { "GetInventoryLevels" },
                PreferredProtocol = "MCP"
            },
            new AgentInfo
            {
                Name = "PricingAgent",
                Role = "Domain",
                EntraIdScope = "SquadCommerce.Pricing.ReadWrite",
                AllowedTools = new[] { "GetInventoryLevels", "UpdateStorePricing" },
                PreferredProtocol = "MCP"
            },
            new AgentInfo
            {
                Name = "MarketIntelAgent",
                Role = "Domain",
                EntraIdScope = "SquadCommerce.MarketIntel.Read",
                AllowedTools = Array.Empty<string>(),
                PreferredProtocol = "A2A"
            }
        };

        return TypedResults.Ok(new AgentListResponse { Agents = agents });
    }

    /// <summary>
    /// Gets the current status of a specific agent.
    /// </summary>
    private static Results<Ok<AgentStatusResponse>, NotFound> GetAgentStatus(string name)
    {
        // Mock implementation - will be replaced with real agent status tracking
        var validAgents = new[] { "ChiefSoftwareArchitect", "InventoryAgent", "PricingAgent", "MarketIntelAgent" };
        
        if (!validAgents.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new AgentStatusResponse
        {
            AgentName = name,
            Status = "Idle",
            LastActivity = DateTimeOffset.UtcNow.AddMinutes(-5),
            ActiveSessions = 0
        });
    }

    /// <summary>
    /// Triggers the competitor price drop analysis scenario.
    /// </summary>
    private static async Task<Accepted<AnalysisResponse>> TriggerAnalysis(
        AnalysisRequest request,
        IAgUiStreamWriter streamWriter,
        SquadCommerceMetrics metrics,
        ILogger<AnalysisRequest> logger,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString();
        logger.LogInformation("Starting competitor price drop analysis: SessionId={SessionId}, Sku={Sku}, TraceId={TraceId}", 
            sessionId, request.Sku, Activity.Current?.TraceId.ToString());

        // Simulate orchestrator triggering analysis workflow
        _ = Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using var orchestratorActivity = metrics.StartAgentSpan("ChiefSoftwareArchitect", "Orchestrate");
                orchestratorActivity?.SetTag("session.id", sessionId);
                orchestratorActivity?.SetTag("sku", request.Sku);

                await streamWriter.WriteStatusUpdateAsync(sessionId, "ChiefSoftwareArchitect orchestrating analysis...", cancellationToken);
                await Task.Delay(500, cancellationToken);

                // MarketIntelAgent phase
                using (var marketIntelActivity = metrics.StartAgentSpan("MarketIntelAgent", "Execute"))
                {
                    marketIntelActivity?.SetTag("session.id", sessionId);
                    logger.LogInformation("MarketIntelAgent executing: SessionId={SessionId}, Sku={Sku}", sessionId, request.Sku);
                    
                    await streamWriter.WriteStatusUpdateAsync(sessionId, "MarketIntelAgent validating competitor pricing via A2A...", cancellationToken);
                    await Task.Delay(800, cancellationToken);
                    
                    metrics.RecordAgentInvocation("MarketIntelAgent", 800, true);
                }

                // InventoryAgent phase
                using (var inventoryActivity = metrics.StartAgentSpan("InventoryAgent", "Execute"))
                {
                    inventoryActivity?.SetTag("session.id", sessionId);
                    logger.LogInformation("InventoryAgent executing: SessionId={SessionId}, Sku={Sku}", sessionId, request.Sku);
                    
                    await streamWriter.WriteStatusUpdateAsync(sessionId, "InventoryAgent querying store inventory via MCP...", cancellationToken);
                    await Task.Delay(600, cancellationToken);
                    
                    metrics.RecordAgentInvocation("InventoryAgent", 600, true);
                }

                // PricingAgent phase
                using (var pricingActivity = metrics.StartAgentSpan("PricingAgent", "Execute"))
                {
                    pricingActivity?.SetTag("session.id", sessionId);
                    logger.LogInformation("PricingAgent executing: SessionId={SessionId}, Sku={Sku}", sessionId, request.Sku);
                    
                    await streamWriter.WriteStatusUpdateAsync(sessionId, "PricingAgent calculating margin impact...", cancellationToken);
                    await Task.Delay(700, cancellationToken);
                    
                    metrics.RecordAgentInvocation("PricingAgent", 700, true);
                }

                await streamWriter.WriteTextDeltaAsync(sessionId, $"Analysis complete for SKU {request.Sku}.", cancellationToken);
                await streamWriter.WriteDoneAsync(sessionId, cancellationToken);

                stopwatch.Stop();
                metrics.RecordAgentInvocation("ChiefSoftwareArchitect", stopwatch.Elapsed.TotalMilliseconds, true);
                
                logger.LogInformation("Analysis workflow completed: SessionId={SessionId}, Duration={DurationMs}ms", 
                    sessionId, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during analysis workflow for session {SessionId}", sessionId);
                metrics.RecordAgentInvocation("ChiefSoftwareArchitect", stopwatch.Elapsed.TotalMilliseconds, false);
            }
        }, cancellationToken);

        return TypedResults.Accepted($"/api/agui?sessionId={sessionId}", new AnalysisResponse
        {
            SessionId = sessionId,
            Message = "Analysis started. Connect to AG-UI stream to receive updates.",
            StreamUrl = $"/api/agui?sessionId={sessionId}"
        });
    }
}

public sealed record AgentListResponse
{
    public required IReadOnlyList<AgentInfo> Agents { get; init; }
}

public sealed record AgentInfo
{
    public required string Name { get; init; }
    public required string Role { get; init; }
    public required string EntraIdScope { get; init; }
    public required IReadOnlyList<string> AllowedTools { get; init; }
    public required string PreferredProtocol { get; init; }
}

public sealed record AgentStatusResponse
{
    public required string AgentName { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset LastActivity { get; init; }
    public required int ActiveSessions { get; init; }
}

public sealed record AnalysisRequest
{
    public required string Sku { get; init; }
    public string? CompetitorName { get; init; }
    public decimal? CompetitorPrice { get; init; }
}

public sealed record AnalysisResponse
{
    public required string SessionId { get; init; }
    public required string Message { get; init; }
    public required string StreamUrl { get; init; }
}
