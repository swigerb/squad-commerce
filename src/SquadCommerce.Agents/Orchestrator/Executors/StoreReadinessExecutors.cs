using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Agents.Orchestrator.Executors;

/// <summary>
/// MAF Executor wrapping TrafficAnalystAgent for foot traffic analysis in the Store Readiness workflow.
/// </summary>
public sealed class TrafficExecutor(TrafficAnalystAgent agent)
    : Executor<StoreReadinessRequest, AgentResult>("Traffic")
{
    public override async ValueTask<AgentResult> HandleAsync(
        StoreReadinessRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(input.StoreId, input.Section, ct);
        await context.QueueStateUpdateAsync("Traffic_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping MerchandisingAgent for planogram optimization in the Store Readiness workflow.
/// </summary>
public sealed class MerchandisingExecutor(MerchandisingAgent agent)
    : Executor<StoreReadinessRequest, AgentResult>("Merchandising")
{
    public override async ValueTask<AgentResult> HandleAsync(
        StoreReadinessRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(input.StoreId, input.Section, ct);
        await context.QueueStateUpdateAsync("Merchandising_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping ManagerAgent (HITL) for approval in the Store Readiness workflow.
/// </summary>
public sealed class ManagerHitlExecutor(ManagerAgent agent)
    : Executor<StoreReadinessRequest, AgentResult>("ManagerHITL")
{
    public override async ValueTask<AgentResult> HandleAsync(
        StoreReadinessRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var merchandisingResult = await context.ReadStateAsync<AgentResult>("Merchandising_Result", ct);
        merchandisingResult ??= new AgentResult
        {
            TextSummary = "No merchandising data available",
            Success = false,
            ErrorMessage = "Merchandising result not found in workflow context",
            Timestamp = DateTimeOffset.UtcNow
        };

        var result = await agent.ExecuteAsync(input.StoreId, input.Section, merchandisingResult, ct);
        await context.QueueStateUpdateAsync("ManagerHITL_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor that synthesizes all Store Readiness agent results into an OrchestratorResult.
/// </summary>
public sealed class StoreReadinessSynthesisExecutor()
    : Executor<StoreReadinessRequest, OrchestratorResult>("StoreReadinessSynthesis")
{
    public override async ValueTask<OrchestratorResult> HandleAsync(
        StoreReadinessRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var trafficResult = await context.ReadStateAsync<AgentResult>("Traffic_Result", ct);
        var merchandisingResult = await context.ReadStateAsync<AgentResult>("Merchandising_Result", ct);
        var managerResult = await context.ReadStateAsync<AgentResult>("ManagerHITL_Result", ct);

        var agentResults = new List<AgentResult>();
        if (trafficResult is not null) agentResults.Add(trafficResult);
        if (merchandisingResult is not null) agentResults.Add(merchandisingResult);
        if (managerResult is not null) agentResults.Add(managerResult);

        var allSucceeded = agentResults.Count > 0 && agentResults.All(r => r.Success);

        var summary = allSucceeded
            ? $"Store readiness analysis for {input.StoreId} section {input.Section} complete. " +
              string.Join(" ", agentResults.Select(r => r.TextSummary))
            : $"Store readiness for {input.StoreId} completed with issues. " +
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
