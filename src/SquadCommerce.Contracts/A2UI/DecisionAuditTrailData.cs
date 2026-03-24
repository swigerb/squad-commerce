namespace SquadCommerce.Contracts.A2UI;

/// <summary>
/// A2UI data contract for Decision Audit Trail Viewer component.
/// Provides chronological view of all agent actions, decisions, and protocol interactions
/// for a given session with full traceability.
/// </summary>
public sealed record DecisionAuditTrailData
{
    /// <summary>
    /// Session identifier for grouping related audit entries.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Ordered list of audit entries (oldest to newest).
    /// </summary>
    public required IReadOnlyList<AuditEntry> Entries { get; init; }

    /// <summary>
    /// Timestamp when this audit trail snapshot was generated.
    /// </summary>
    public required DateTimeOffset GeneratedAt { get; init; }
}

/// <summary>
/// Individual audit entry representing a single agent action or decision.
/// </summary>
public sealed record AuditEntry
{
    /// <summary>
    /// Unique identifier for this audit entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Name of the agent that performed this action.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// Description of the action performed.
    /// Examples: "Queried inventory", "Validated competitor data", "Updated pricing"
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Protocol used for this action.
    /// Values: MCP, A2A, AGUI, Internal
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// When this action was initiated.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// How long this action took to complete.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Outcome status of this action.
    /// Values: Success, Failed, Warning
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Optional additional context about this action.
    /// Can include error messages, validation results, or business logic notes.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// OpenTelemetry trace ID for correlation with distributed tracing systems.
    /// Allows deep drill-down into execution details.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// SKUs affected by this action (if applicable).
    /// Useful for filtering audit trail by product.
    /// </summary>
    public IReadOnlyList<string>? AffectedSkus { get; init; }

    /// <summary>
    /// Store IDs affected by this action (if applicable).
    /// Useful for filtering audit trail by store.
    /// </summary>
    public IReadOnlyList<string>? AffectedStores { get; init; }

    /// <summary>
    /// For human approval/rejection actions, captures the decision made.
    /// Examples: "Approved", "Rejected", "Modified to $24.99"
    /// </summary>
    public string? DecisionOutcome { get; init; }
}
