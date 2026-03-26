using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Orchestrator.Executors;

namespace SquadCommerce.Agents.Orchestrator;

/// <summary>
/// MAF Graph-based Workflow definition for the Store Readiness scenario.
/// Builds a linear pipeline: Traffic → Merchandising → ManagerHITL → Synthesis.
/// </summary>
public sealed class StoreReadinessWorkflow
{
    private readonly TrafficExecutor _traffic;
    private readonly MerchandisingExecutor _merchandising;
    private readonly ManagerHitlExecutor _managerHitl;
    private readonly StoreReadinessSynthesisExecutor _synthesis;

    public StoreReadinessWorkflow(
        TrafficExecutor traffic,
        MerchandisingExecutor merchandising,
        ManagerHitlExecutor managerHitl,
        StoreReadinessSynthesisExecutor synthesis)
    {
        _traffic = traffic;
        _merchandising = merchandising;
        _managerHitl = managerHitl;
        _synthesis = synthesis;
    }

    /// <summary>
    /// Builds the MAF workflow graph for the store readiness pipeline.
    /// </summary>
    public Workflow Build()
    {
        var builder = new WorkflowBuilder(_traffic);
        builder.AddEdge(_traffic, _merchandising);
        builder.AddEdge(_merchandising, _managerHitl);
        builder.AddEdge(_managerHitl, _synthesis);
        builder.WithOutputFrom(_synthesis);
        return builder.Build();
    }
}
