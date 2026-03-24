namespace SquadCommerce.Contracts.A2UI;

/// <summary>
/// A2UI data contract for Agent Pipeline Visualizer component.
/// Provides real-time visualization of multi-stage agent workflows showing
/// stage transitions, protocols used, and outputs generated.
/// </summary>
public sealed record AgentPipelineData
{
    /// <summary>
    /// Session identifier for this workflow execution.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Human-readable name of this workflow.
    /// Examples: "CompetitorPriceDropWorkflow", "InventoryRebalanceWorkflow"
    /// </summary>
    public required string WorkflowName { get; init; }

    /// <summary>
    /// Ordered stages in this pipeline (execution order).
    /// </summary>
    public required IReadOnlyList<PipelineStage> Stages { get; init; }

    /// <summary>
    /// Overall workflow status.
    /// Values: Pending, Running, Completed, Failed
    /// </summary>
    public required string OverallStatus { get; init; }

    /// <summary>
    /// Total time taken for the entire workflow.
    /// For running workflows, this is time elapsed so far.
    /// </summary>
    public required TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// When the workflow was initiated.
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// When the workflow completed (null if still running or failed mid-execution).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }
}

/// <summary>
/// Individual stage in an agent pipeline.
/// Stages execute sequentially (or in parallel in future implementations).
/// </summary>
public sealed record PipelineStage
{
    /// <summary>
    /// Execution order (1-based index).
    /// </summary>
    public required int Order { get; init; }

    /// <summary>
    /// Agent responsible for this stage.
    /// Examples: "MarketIntelAgent", "InventoryAgent", "PricingAgent"
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// Human-readable name for this stage.
    /// Examples: "Market Intelligence", "Inventory Analysis", "Pricing Calculation"
    /// </summary>
    public required string StageName { get; init; }

    /// <summary>
    /// Current status of this stage.
    /// Values: Pending, Running, Completed, Failed, Skipped
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Protocol used by this agent.
    /// Values: MCP, A2A, Internal, AGUI
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// How long this stage took to execute (null if not started or still running).
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// When this stage started executing (null if not started yet).
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// When this stage completed (null if not completed yet).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// MCP tools or A2A methods invoked during this stage.
    /// Examples: ["GetInventoryLevels", "UpdateStorePricing"]
    /// </summary>
    public IReadOnlyList<string>? ToolsUsed { get; init; }

    /// <summary>
    /// A2UI component names produced by this stage for downstream rendering.
    /// Examples: ["MarketComparisonGrid", "RetailStockHeatmap"]
    /// </summary>
    public IReadOnlyList<string>? OutputPayloads { get; init; }

    /// <summary>
    /// Error message if this stage failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
