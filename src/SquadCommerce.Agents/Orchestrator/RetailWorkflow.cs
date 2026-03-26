using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Orchestrator.Executors;

namespace SquadCommerce.Agents.Orchestrator;

/// <summary>
/// MAF Graph-based Workflow definition for retail supply chain orchestration.
/// Builds a linear pipeline: MarketIntel → Inventory → Pricing → Synthesis.
/// </summary>
public sealed class RetailWorkflow
{
    private readonly MarketIntelExecutor _marketIntel;
    private readonly InventoryExecutor _inventory;
    private readonly PricingExecutor _pricing;
    private readonly SynthesisExecutor _synthesis;

    public RetailWorkflow(
        MarketIntelExecutor marketIntel,
        InventoryExecutor inventory,
        PricingExecutor pricing,
        SynthesisExecutor synthesis)
    {
        _marketIntel = marketIntel;
        _inventory = inventory;
        _pricing = pricing;
        _synthesis = synthesis;
    }

    /// <summary>
    /// Builds the MAF workflow graph for the competitor price response pipeline.
    /// </summary>
    public Workflow Build()
    {
        var builder = new WorkflowBuilder(_marketIntel);
        builder.AddEdge(_marketIntel, _inventory);
        builder.AddEdge(_inventory, _pricing);
        builder.AddEdge(_pricing, _synthesis);
        builder.WithOutputFrom(_synthesis);
        return builder.Build();
    }
}
