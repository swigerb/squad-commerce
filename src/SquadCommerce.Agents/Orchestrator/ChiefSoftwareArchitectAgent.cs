using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Orchestrator;

/// <summary>
/// ChiefSoftwareArchitect is the orchestrator agent for Squad-Commerce.
/// It receives user requests, decomposes them into tasks, delegates to domain agents,
/// and synthesizes the final response.
/// </summary>
/// <remarks>
/// This agent NEVER calls MCP tools directly. It only delegates to:
/// - InventoryAgent (inventory queries)
/// - PricingAgent (pricing calculations and updates)
/// - MarketIntelAgent (competitor intelligence via A2A)
/// 
/// Allowed tools: [] (orchestrators delegate only)
/// Required scope: SquadCommerce.Orchestrate
/// </remarks>
public sealed class ChiefSoftwareArchitectAgent
{
    private readonly InventoryAgent _inventoryAgent;
    private readonly PricingAgent _pricingAgent;
    private readonly MarketIntelAgent _marketIntelAgent;
    private readonly ILogger<ChiefSoftwareArchitectAgent> _logger;

    public ChiefSoftwareArchitectAgent(
        InventoryAgent inventoryAgent,
        PricingAgent pricingAgent,
        MarketIntelAgent marketIntelAgent,
        ILogger<ChiefSoftwareArchitectAgent> logger)
    {
        _inventoryAgent = inventoryAgent ?? throw new ArgumentNullException(nameof(inventoryAgent));
        _pricingAgent = pricingAgent ?? throw new ArgumentNullException(nameof(pricingAgent));
        _marketIntelAgent = marketIntelAgent ?? throw new ArgumentNullException(nameof(marketIntelAgent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Orchestrates a competitor price response workflow using graph-based delegation.
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <param name="competitorPrice">Competitor's claimed price</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Orchestrated result with all A2UI payloads and executive summary</returns>
    public async Task<OrchestratorResult> ProcessCompetitorPriceDropAsync(
        string sku,
        decimal competitorPrice,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;
        
        // Create parent orchestrator span that wraps entire workflow
        using var activity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Orchestrate");
        activity?.SetTag("agent.name", "ChiefSoftwareArchitect");
        activity?.SetTag("agent.protocol", "AGUI");
        activity?.SetTag("agent.sku", sku);
        activity?.SetTag("agent.competitor_price", competitorPrice);
        
        // Record invocation count
        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));

        _logger.LogInformation(
            "Orchestrator starting competitor price response workflow: SKU {Sku}, CompetitorPrice ${CompetitorPrice:F2}",
            sku,
            competitorPrice);

        var results = new List<AgentResult>();

        try
        {
            // Step 1: Validate competitor claim via A2A (MarketIntelAgent)
            _logger.LogInformation("Step 1: Delegating to MarketIntelAgent for competitor validation");
            var marketIntelResult = await _marketIntelAgent.ExecuteAsync(
                sku,
                competitorPrice,
                cancellationToken);
            results.Add(marketIntelResult);

            if (!marketIntelResult.Success)
            {
                _logger.LogWarning("MarketIntelAgent failed - aborting workflow");
                return BuildFailureResult(results, "Failed to validate competitor pricing", startTime);
            }

            // Step 2: Get inventory snapshot (InventoryAgent)
            _logger.LogInformation("Step 2: Delegating to InventoryAgent for inventory snapshot");
            var inventoryResult = await _inventoryAgent.ExecuteAsync(sku, cancellationToken);
            results.Add(inventoryResult);

            if (!inventoryResult.Success)
            {
                _logger.LogWarning("InventoryAgent failed - continuing with limited data");
            }

            // Step 3: Calculate margin impact (PricingAgent)
            _logger.LogInformation("Step 3: Delegating to PricingAgent for margin impact analysis");
            var pricingResult = await _pricingAgent.ExecuteAsync(
                sku,
                competitorPrice,
                cancellationToken);
            results.Add(pricingResult);

            if (!pricingResult.Success)
            {
                _logger.LogWarning("PricingAgent failed - aborting workflow");
                return BuildFailureResult(results, "Failed to calculate pricing impact", startTime);
            }

            // Step 4: Synthesize final response
            _logger.LogInformation("Step 4: Synthesizing orchestrator response");
            
            // Create synthesize span as child of orchestrate
            using var synthesizeActivity = SquadCommerceTelemetry.StartAgentSpan("ChiefSoftwareArchitect", "Synthesize");
            synthesizeActivity?.SetTag("agent.result_count", results.Count);
            
            var executiveSummary = BuildExecutiveSummary(sku, competitorPrice, results);

            var duration = DateTimeOffset.UtcNow - startTime;
            
            // Record orchestrator invocation duration
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration.TotalMilliseconds,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));
            
            _logger.LogInformation(
                "Orchestrator workflow completed successfully in {Duration}ms",
                duration.TotalMilliseconds);

            return new OrchestratorResult
            {
                Success = true,
                ExecutiveSummary = executiveSummary,
                AgentResults = results,
                Timestamp = DateTimeOffset.UtcNow,
                WorkflowDuration = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestrator workflow failed");
            
            // Set error status on span
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            
            // Record duration even on error
            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", "ChiefSoftwareArchitect"));
            
            return BuildFailureResult(results, $"Orchestration error: {ex.Message}", startTime);
        }
    }

    private static string BuildExecutiveSummary(string sku, decimal competitorPrice, List<AgentResult> results)
    {
        var summary = $"## Competitor Price Response Analysis for {sku}\n\n";
        summary += $"**Competitor Price:** ${competitorPrice:F2}\n\n";

        foreach (var result in results)
        {
            summary += $"### {result.GetType().Name}\n";
            summary += $"{result.TextSummary}\n\n";
        }

        summary += "**Recommendation:** Review the pricing impact scenarios above and select the optimal strategy. " +
                   "All competitor data has been validated via A2A protocol and cross-referenced against internal benchmarks.";

        return summary;
    }

    private OrchestratorResult BuildFailureResult(List<AgentResult> results, string errorMessage, DateTimeOffset startTime)
    {
        var duration = DateTimeOffset.UtcNow - startTime;
        return new OrchestratorResult
        {
            Success = false,
            ExecutiveSummary = $"Workflow failed: {errorMessage}",
            AgentResults = results,
            ErrorMessage = errorMessage,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = duration
        };
    }
}

/// <summary>
/// Result from orchestrator execution containing all agent results and synthesis.
/// </summary>
public sealed record OrchestratorResult
{
    /// <summary>
    /// Overall workflow success status.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Executive summary synthesizing all agent results.
    /// </summary>
    public required string ExecutiveSummary { get; init; }

    /// <summary>
    /// Individual results from each agent execution.
    /// </summary>
    public required IReadOnlyList<AgentResult> AgentResults { get; init; }

    /// <summary>
    /// Error message if Success is false.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Timestamp of orchestration completion.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Total workflow duration.
    /// </summary>
    public required TimeSpan WorkflowDuration { get; init; }
}
