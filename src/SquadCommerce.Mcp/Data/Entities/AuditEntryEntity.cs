namespace SquadCommerce.Mcp.Data.Entities;

/// <summary>
/// EF Core entity representing audit trail entries.
/// Maps to AuditEntries table with single primary key (Id).
/// Records all agent actions, decisions, and protocol interactions for compliance and debugging.
/// </summary>
public sealed class AuditEntryEntity
{
    /// <summary>
    /// Unique identifier for this audit entry (GUID).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Session identifier for grouping related audit entries.
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Name of the agent that performed this action.
    /// </summary>
    public required string AgentName { get; set; }

    /// <summary>
    /// Description of the action performed.
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// Protocol used for this action (MCP, A2A, AGUI, Internal).
    /// </summary>
    public required string Protocol { get; set; }

    /// <summary>
    /// When this action was initiated (UTC).
    /// </summary>
    public required DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Duration of this action in milliseconds.
    /// </summary>
    public required long DurationMs { get; set; }

    /// <summary>
    /// Outcome status (Success, Failed, Warning).
    /// </summary>
    public required string Status { get; set; }

    /// <summary>
    /// Optional additional context (error messages, validation notes, etc.).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// OpenTelemetry trace ID for correlation.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// For human approval/rejection actions, captures the decision made.
    /// </summary>
    public string? DecisionOutcome { get; set; }

    /// <summary>
    /// Comma-separated list of affected SKUs (null if not applicable).
    /// </summary>
    public string? AffectedSkusCsv { get; set; }

    /// <summary>
    /// Comma-separated list of affected store IDs (null if not applicable).
    /// </summary>
    public string? AffectedStoresCsv { get; set; }
}
