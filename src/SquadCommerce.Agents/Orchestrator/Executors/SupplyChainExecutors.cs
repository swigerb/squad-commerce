using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Agents.Orchestrator.Executors;

/// <summary>
/// MAF Executor wrapping LogisticsAgent for shipment delay analysis in the Supply Chain Shock workflow.
/// </summary>
public sealed class LogisticsExecutor(LogisticsAgent agent)
    : Executor<SupplyChainShockRequest, AgentResult>("Logistics")
{
    public override async ValueTask<AgentResult> HandleAsync(
        SupplyChainShockRequest message, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(message.Sku, message.DelayDays, message.Reason, ct);
        await context.QueueStateUpdateAsync("Logistics_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping InventoryAgent for stock level queries in the Supply Chain Shock workflow.
/// Reuses the existing InventoryAgent.
/// </summary>
public sealed class SupplyChainInventoryExecutor(InventoryAgent agent)
    : Executor<SupplyChainShockRequest, AgentResult>("SupplyChainInventory")
{
    public override async ValueTask<AgentResult> HandleAsync(
        SupplyChainShockRequest message, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(message.Sku, ct);
        await context.QueueStateUpdateAsync("SupplyChainInventory_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping RedistributionAgent for stock redistribution in the Supply Chain Shock workflow.
/// </summary>
public sealed class RedistributionExecutor(RedistributionAgent agent)
    : Executor<SupplyChainShockRequest, AgentResult>("Redistribution")
{
    public override async ValueTask<AgentResult> HandleAsync(
        SupplyChainShockRequest message, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(message.Sku, message.AffectedRegions, ct);
        await context.QueueStateUpdateAsync("Redistribution_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor that synthesizes all Supply Chain Shock agent results into an OrchestratorResult.
/// </summary>
public sealed class SupplyChainSynthesisExecutor()
    : Executor<SupplyChainShockRequest, OrchestratorResult>("SupplyChainSynthesis")
{
    public override async ValueTask<OrchestratorResult> HandleAsync(
        SupplyChainShockRequest message, IWorkflowContext context, CancellationToken ct)
    {
        var logisticsResult = await context.ReadStateAsync<AgentResult>("Logistics_Result", ct);
        var inventoryResult = await context.ReadStateAsync<AgentResult>("SupplyChainInventory_Result", ct);
        var redistributionResult = await context.ReadStateAsync<AgentResult>("Redistribution_Result", ct);

        var agentResults = new List<AgentResult>();
        if (logisticsResult is not null) agentResults.Add(logisticsResult);
        if (inventoryResult is not null) agentResults.Add(inventoryResult);
        if (redistributionResult is not null) agentResults.Add(redistributionResult);

        var allSucceeded = agentResults.Count > 0 && agentResults.All(r => r.Success);

        var summary = allSucceeded
            ? $"Supply chain shock analysis for SKU {message.Sku} complete. " +
              $"Delay: {message.DelayDays} days due to {message.Reason}. " +
              string.Join(" ", agentResults.Select(r => r.TextSummary))
            : $"Supply chain shock analysis for SKU {message.Sku} completed with errors. " +
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
