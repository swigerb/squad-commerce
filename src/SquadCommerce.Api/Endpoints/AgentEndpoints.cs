using Microsoft.AspNetCore.Http.HttpResults;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Agents.Policies;
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
        var policies = AgentPolicyRegistry.GetAllPolicies();
        
        var agents = policies.Select(policy => new AgentInfo
        {
            Name = policy.AgentName,
            Role = policy.AgentName == "ChiefSoftwareArchitect" ? "Orchestrator" : "Domain",
            EntraIdScope = policy.EntraIdScope,
            AllowedTools = policy.AllowedTools.ToArray(),
            PreferredProtocol = policy.PreferredProtocol
        }).ToArray();

        return TypedResults.Ok(new AgentListResponse { Agents = agents });
    }

    /// <summary>
    /// Gets the current status of a specific agent.
    /// </summary>
    private static Results<Ok<AgentStatusResponse>, NotFound> GetAgentStatus(string name)
    {
        var policy = AgentPolicyRegistry.GetPolicyByName(name);
        
        if (policy == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new AgentStatusResponse
        {
            AgentName = policy.AgentName,
            Status = "Idle",
            LastActivity = DateTimeOffset.UtcNow.AddMinutes(-5),
            ActiveSessions = 0
        });
    }

    /// <summary>
    /// Triggers the competitor price drop analysis scenario.
    /// </summary>
    private static async Task<Results<Accepted<AnalysisResponse>, BadRequest<string>>> TriggerAnalysis(
        AnalysisRequest request,
        IAgUiStreamWriter streamWriter,
        IServiceProvider serviceProvider,
        SquadCommerceMetrics metrics,
        ILogger<AnalysisRequest> logger,
        CancellationToken cancellationToken)
    {
        // Validate CompetitorPrice
        if (!request.CompetitorPrice.HasValue || request.CompetitorPrice.Value <= 0)
        {
            return TypedResults.BadRequest("CompetitorPrice is required and must be greater than zero.");
        }

        var sessionId = Guid.NewGuid().ToString();
        logger.LogInformation("Starting competitor price drop analysis: SessionId={SessionId}, Sku={Sku}, CompetitorPrice=${CompetitorPrice:F2}, TraceId={TraceId}", 
            sessionId, request.Sku, request.CompetitorPrice.Value, Activity.Current?.TraceId.ToString());

        // Orchestrate analysis workflow in background with proper DI scoping
        _ = Task.Run(async () =>
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // Create a new scope for background work to resolve scoped services
                using var scope = serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<ChiefSoftwareArchitectAgent>();
                
                logger.LogInformation("Invoking ChiefSoftwareArchitectAgent for session {SessionId}", sessionId);
                
                // Stream status before orchestration starts
                await streamWriter.WriteStatusUpdateAsync(sessionId, "ChiefSoftwareArchitect orchestrating analysis...", cancellationToken);
                
                // Call real orchestrator
                var result = await orchestrator.ProcessCompetitorPriceDropAsync(
                    request.Sku, 
                    request.CompetitorPrice.Value, 
                    cancellationToken);
                
                // Check if orchestration succeeded
                if (!result.Success)
                {
                    logger.LogError("Orchestration failed for session {SessionId}: {ErrorMessage}", sessionId, result.ErrorMessage);
                    await streamWriter.WriteStatusUpdateAsync(sessionId, $"Error: {result.ErrorMessage}", cancellationToken);
                    await streamWriter.WriteTextDeltaAsync(sessionId, $"Analysis failed: {result.ErrorMessage}", cancellationToken);
                    await streamWriter.WriteDoneAsync(sessionId, cancellationToken);
                    
                    stopwatch.Stop();
                    metrics.RecordAgentInvocation("ChiefSoftwareArchitect", stopwatch.Elapsed.TotalMilliseconds, false);
                    return;
                }
                
                // Stream A2UI payloads from each agent result
                foreach (var agentResult in result.AgentResults)
                {
                    if (agentResult.A2UIPayload != null)
                    {
                        await streamWriter.WriteA2UIPayloadAsync(sessionId, agentResult.A2UIPayload, cancellationToken);
                        logger.LogInformation("Streamed A2UI payload for session {SessionId}", sessionId);
                    }
                }
                
                // Stream executive summary as text delta
                await streamWriter.WriteTextDeltaAsync(sessionId, result.ExecutiveSummary, cancellationToken);
                
                // Signal completion
                await streamWriter.WriteDoneAsync(sessionId, cancellationToken);

                stopwatch.Stop();
                logger.LogInformation("Analysis workflow completed: SessionId={SessionId}, Duration={DurationMs}ms", 
                    sessionId, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during analysis workflow for session {SessionId}", sessionId);
                
                try
                {
                    await streamWriter.WriteStatusUpdateAsync(sessionId, $"Error: {ex.Message}", cancellationToken);
                    await streamWriter.WriteTextDeltaAsync(sessionId, $"An unexpected error occurred: {ex.Message}", cancellationToken);
                    await streamWriter.WriteDoneAsync(sessionId, cancellationToken);
                }
                catch
                {
                    // Best effort - don't throw in cleanup
                }
                
                stopwatch.Stop();
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
