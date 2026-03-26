using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Orchestrator.Executors;

namespace SquadCommerce.Agents.Orchestrator;

/// <summary>
/// MAF Graph-based Workflow definition for the Supply Chain Shock scenario.
/// Builds a linear pipeline: Logistics → Inventory → Redistribution → Synthesis.
/// </summary>
public sealed class SupplyChainWorkflow
{
    private readonly LogisticsExecutor _logistics;
    private readonly SupplyChainInventoryExecutor _inventory;
    private readonly RedistributionExecutor _redistribution;
    private readonly SupplyChainSynthesisExecutor _synthesis;

    public SupplyChainWorkflow(
        LogisticsExecutor logistics,
        SupplyChainInventoryExecutor inventory,
        RedistributionExecutor redistribution,
        SupplyChainSynthesisExecutor synthesis)
    {
        _logistics = logistics;
        _inventory = inventory;
        _redistribution = redistribution;
        _synthesis = synthesis;
    }

    /// <summary>
    /// Builds the MAF workflow graph for the supply chain shock response pipeline.
    /// </summary>
    public Workflow Build()
    {
        var builder = new WorkflowBuilder(_logistics);
        builder.AddEdge(_logistics, _inventory);
        builder.AddEdge(_inventory, _redistribution);
        builder.AddEdge(_redistribution, _synthesis);
        builder.WithOutputFrom(_synthesis);
        return builder.Build();
    }
}
