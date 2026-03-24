using Microsoft.AspNetCore.Http.HttpResults;
using SquadCommerce.Api.Services;

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
        ILogger<AnalysisRequest> logger,
        CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString();
        logger.LogInformation("Starting competitor price drop analysis: SessionId={SessionId}, Sku={Sku}", sessionId, request.Sku);

        // Simulate orchestrator triggering analysis workflow
        _ = Task.Run(async () =>
        {
            try
            {
                await streamWriter.WriteStatusUpdateAsync(sessionId, "ChiefSoftwareArchitect orchestrating analysis...", cancellationToken);
                await Task.Delay(500, cancellationToken);

                await streamWriter.WriteStatusUpdateAsync(sessionId, "MarketIntelAgent validating competitor pricing via A2A...", cancellationToken);
                await Task.Delay(800, cancellationToken);

                await streamWriter.WriteStatusUpdateAsync(sessionId, "InventoryAgent querying store inventory via MCP...", cancellationToken);
                await Task.Delay(600, cancellationToken);

                await streamWriter.WriteStatusUpdateAsync(sessionId, "PricingAgent calculating margin impact...", cancellationToken);
                await Task.Delay(700, cancellationToken);

                await streamWriter.WriteTextDeltaAsync(sessionId, $"Analysis complete for SKU {request.Sku}.", cancellationToken);
                await streamWriter.WriteDoneAsync(sessionId, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during analysis workflow for session {SessionId}", sessionId);
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
