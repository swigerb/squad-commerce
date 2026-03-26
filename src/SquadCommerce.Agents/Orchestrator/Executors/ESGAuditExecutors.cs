using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Agents.Orchestrator.Executors;

/// <summary>
/// MAF Executor wrapping ComplianceAgent for supplier certification analysis in the ESG Audit workflow.
/// </summary>
public sealed class ComplianceExecutor(ComplianceAgent agent)
    : Executor<ESGAuditRequest, AgentResult>("Compliance")
{
    public override async ValueTask<AgentResult> HandleAsync(
        ESGAuditRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(input.ProductCategory, input.CertificationRequired, input.Deadline, ct);
        await context.QueueStateUpdateAsync("Compliance_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping ResearchAgent for sustainability watchlist cross-referencing in the ESG Audit workflow.
/// </summary>
public sealed class ResearchExecutor(ResearchAgent agent)
    : Executor<ESGAuditRequest, AgentResult>("Research")
{
    public override async ValueTask<AgentResult> HandleAsync(
        ESGAuditRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(input.ProductCategory, ct);
        await context.QueueStateUpdateAsync("Research_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor wrapping ProcurementAgent for alternative supplier identification in the ESG Audit workflow.
/// </summary>
public sealed class ProcurementExecutor(ProcurementAgent agent)
    : Executor<ESGAuditRequest, AgentResult>("Procurement")
{
    public override async ValueTask<AgentResult> HandleAsync(
        ESGAuditRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var result = await agent.ExecuteAsync(input.ProductCategory, input.CertificationRequired, ct);
        await context.QueueStateUpdateAsync("Procurement_Result", result, ct);
        return result;
    }
}

/// <summary>
/// MAF Executor that synthesizes all ESG Audit agent results into an OrchestratorResult.
/// </summary>
public sealed class ESGSynthesisExecutor()
    : Executor<ESGAuditRequest, OrchestratorResult>("ESGSynthesis")
{
    public override async ValueTask<OrchestratorResult> HandleAsync(
        ESGAuditRequest input, IWorkflowContext context, CancellationToken ct)
    {
        var complianceResult = await context.ReadStateAsync<AgentResult>("Compliance_Result", ct);
        var researchResult = await context.ReadStateAsync<AgentResult>("Research_Result", ct);
        var procurementResult = await context.ReadStateAsync<AgentResult>("Procurement_Result", ct);

        var agentResults = new List<AgentResult>();
        if (complianceResult is not null) agentResults.Add(complianceResult);
        if (researchResult is not null) agentResults.Add(researchResult);
        if (procurementResult is not null) agentResults.Add(procurementResult);

        var allSucceeded = agentResults.Count > 0 && agentResults.All(r => r.Success);

        var summary = allSucceeded
            ? $"ESG audit for {input.ProductCategory} ({input.CertificationRequired}) complete. " +
              string.Join(" ", agentResults.Select(r => r.TextSummary))
            : $"ESG audit for {input.ProductCategory} completed with errors. " +
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
