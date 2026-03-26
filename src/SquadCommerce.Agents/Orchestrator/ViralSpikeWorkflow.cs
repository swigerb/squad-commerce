using Microsoft.Agents.AI.Workflows;
using SquadCommerce.Agents.Orchestrator.Executors;

namespace SquadCommerce.Agents.Orchestrator;

/// <summary>
/// MAF Graph-based Workflow definition for the Viral Spike scenario.
/// Builds a linear pipeline: Sentiment → Pricing → Marketing → Synthesis.
/// </summary>
public sealed class ViralSpikeWorkflow
{
    private readonly ViralSpikeSentimentExecutor _sentiment;
    private readonly ViralSpikePricingExecutor _pricing;
    private readonly ViralSpikeMarketingExecutor _marketing;
    private readonly ViralSpikeSynthesisExecutor _synthesis;

    public ViralSpikeWorkflow(
        ViralSpikeSentimentExecutor sentiment,
        ViralSpikePricingExecutor pricing,
        ViralSpikeMarketingExecutor marketing,
        ViralSpikeSynthesisExecutor synthesis)
    {
        _sentiment = sentiment;
        _pricing = pricing;
        _marketing = marketing;
        _synthesis = synthesis;
    }

    /// <summary>
    /// Builds the MAF workflow graph for the viral spike response pipeline.
    /// </summary>
    public Workflow Build()
    {
        var builder = new WorkflowBuilder(_sentiment);
        builder.AddEdge(_sentiment, _pricing);
        builder.AddEdge(_pricing, _marketing);
        builder.AddEdge(_marketing, _synthesis);
        builder.WithOutputFrom(_synthesis);
        return builder.Build();
    }
}
