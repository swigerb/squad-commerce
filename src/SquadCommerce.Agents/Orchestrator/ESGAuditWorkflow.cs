using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Orchestrator.Executors;

namespace SquadCommerce.Agents.Orchestrator;

/// <summary>
/// MAF Graph-based Workflow definition for the ESG Audit scenario.
/// Builds a linear pipeline: Compliance → Research → Procurement → Synthesis.
/// </summary>
public sealed class ESGAuditWorkflow
{
    private readonly ComplianceExecutor _compliance;
    private readonly ResearchExecutor _research;
    private readonly ProcurementExecutor _procurement;
    private readonly ESGSynthesisExecutor _synthesis;

    public ESGAuditWorkflow(
        ComplianceExecutor compliance,
        ResearchExecutor research,
        ProcurementExecutor procurement,
        ESGSynthesisExecutor synthesis)
    {
        _compliance = compliance;
        _research = research;
        _procurement = procurement;
        _synthesis = synthesis;
    }

    /// <summary>
    /// Builds the MAF workflow graph for the ESG audit pipeline.
    /// </summary>
    public Workflow Build()
    {
        var builder = new WorkflowBuilder(_compliance);
        builder.AddEdge(_compliance, _research);
        builder.AddEdge(_research, _procurement);
        builder.AddEdge(_procurement, _synthesis);
        builder.WithOutputFrom(_synthesis);
        return builder.Build();
    }
}
