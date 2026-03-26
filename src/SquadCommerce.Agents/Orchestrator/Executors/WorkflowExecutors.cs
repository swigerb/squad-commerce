using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Domain;

namespace SquadCommerce.Agents.Orchestrator.Executors;

/// <summary>
/// Input request for the competitor price drop workflow.
/// </summary>
public sealed record CompetitorPriceDropRequest
{
    public required string Sku { get; init; }
    public required decimal CompetitorPrice { get; init; }
    public required string SessionId { get; init; }
    public string? CompetitorName { get; init; }
}

/// <summary>
/// MAF Executor wrapping MarketIntelAgent for competitor pricing validation via A2A.
/// </summary>
public sealed class MarketIntelExecutor(MarketIntelAgent agent)
    : Executor<CompetitorPriceDropRequest, AgentResult>("MarketIntel")
{
    public override async ValueTask<AgentResult> HandleAsync(
        CompetitorPriceDropRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(input.Sku, input.CompetitorPrice, ct);
        await context.QueueStateUpdateAsync("MarketIntel_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping InventoryAgent for stock level queries via MCP.
/// </summary>
public sealed class InventoryExecutor(InventoryAgent agent)
    : Executor<CompetitorPriceDropRequest, AgentResult>("Inventory")
{
    public override async ValueTask<AgentResult> HandleAsync(
        CompetitorPriceDropRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(input.Sku, ct);
        await context.QueueStateUpdateAsync("Inventory_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping PricingAgent for margin impact analysis via MCP.
/// </summary>
public sealed class PricingExecutor(PricingAgent agent)
    : Executor<CompetitorPriceDropRequest, AgentResult>("Pricing")
{
    public override async ValueTask<AgentResult> HandleAsync(
        CompetitorPriceDropRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(input.Sku, input.CompetitorPrice, ct);
        await context.QueueStateUpdateAsync("Pricing_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor that synthesizes all agent results into an OrchestratorResult.
/// </summary>
public sealed class SynthesisExecutor()
    : Executor<CompetitorPriceDropRequest, OrchestratorResult>("Synthesis")
{
    public override async ValueTask<OrchestratorResult> HandleAsync(
        CompetitorPriceDropRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var marketIntelResult = await context.ReadStateAsync<AgentResult>("MarketIntel_Result", ct);
        var inventoryResult = await context.ReadStateAsync<AgentResult>("Inventory_Result", ct);
        var pricingResult = await context.ReadStateAsync<AgentResult>("Pricing_Result", ct);

        var agentResults = new List<AgentResult>();
        if (marketIntelResult is not null) agentResults.Add(marketIntelResult);
        if (inventoryResult is not null) agentResults.Add(inventoryResult);
        if (pricingResult is not null) agentResults.Add(pricingResult);

        var allSucceeded = agentResults.Count > 0 && agentResults.All(r => r.Success);

        var summary = allSucceeded
            ? $"Competitor price drop analysis for SKU {input.Sku} complete. " +
              string.Join(" ", agentResults.Select(r => r.TextSummary))
            : $"Analysis for SKU {input.Sku} completed with errors. " +
              string.Join(" ", agentResults.Where(r => !r.Success).Select(r => r.ErrorMessage));

        return new OrchestratorResult
        {
            Success = allSucceeded,
            ExecutiveSummary = summary,
            AgentResults = agentResults,
            Timestamp = DateTimeOffset.UtcNow,
            WorkflowDuration = TimeSpan.Zero
        };
    }
}
