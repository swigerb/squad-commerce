using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Agents.Orchestrator.Executors;

/// <summary>
/// MAF Executor wrapping MarketIntelAgent for social sentiment analysis in the Viral Spike workflow.
/// </summary>
public sealed class ViralSpikeSentimentExecutor(MarketIntelAgent agent)
    : Executor<ViralSpikeRequest, AgentResult>("ViralSpikeSentiment")
{
    public override async ValueTask<AgentResult> HandleAsync(
        ViralSpikeRequest message, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.AnalyzeSocialSentimentAsync(message.Sku, message.Region, ct);
        await context.QueueStateUpdateAsync("SentimentResult", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping PricingAgent for flash sale pricing in the Viral Spike workflow.
/// </summary>
public sealed class ViralSpikePricingExecutor(PricingAgent agent)
    : Executor<ViralSpikeRequest, AgentResult>("ViralSpikePricing")
{
    public override async ValueTask<AgentResult> HandleAsync(
        ViralSpikeRequest message, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.CalculateFlashSalePricingAsync(
            message.Sku, message.DemandMultiplier, message.Region, ct);
        await context.QueueStateUpdateAsync("PricingResult", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping MarketingAgent for campaign preview in the Viral Spike workflow.
/// </summary>
public sealed class ViralSpikeMarketingExecutor(MarketingAgent agent)
    : Executor<ViralSpikeRequest, AgentResult>("ViralSpikeMarketing")
{
    public override async ValueTask<AgentResult> HandleAsync(
        ViralSpikeRequest message, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(
            message.Sku, message.DemandMultiplier, message.Region, ct);
        await context.QueueStateUpdateAsync("MarketingResult", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor that synthesizes all Viral Spike agent results into an OrchestratorResult.
/// </summary>
public sealed class ViralSpikeSynthesisExecutor()
    : Executor<ViralSpikeRequest, OrchestratorResult>("ViralSpikeSynthesis")
{
    public override async ValueTask<OrchestratorResult> HandleAsync(
        ViralSpikeRequest message, IWorkflowContext context, CancellationToken ct)
    {
        var sentimentResult = await context.ReadStateAsync<AgentResult>("SentimentResult", ct);
        var pricingResult = await context.ReadStateAsync<AgentResult>("PricingResult", ct);
        var marketingResult = await context.ReadStateAsync<AgentResult>("MarketingResult", ct);

        var agentResults = new List<AgentResult>();
        if (sentimentResult is not null) agentResults.Add(sentimentResult);
        if (pricingResult is not null) agentResults.Add(pricingResult);
        if (marketingResult is not null) agentResults.Add(marketingResult);

        var allSucceeded = agentResults.Count > 0 && agentResults.All(r => r.Success);

        var summary = allSucceeded
            ? $"Viral spike analysis for SKU {message.Sku} in {message.Region} complete. " +
              string.Join(" ", agentResults.Select(r => r.TextSummary))
            : $"Viral spike analysis for SKU {message.Sku} completed with errors. " +
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
