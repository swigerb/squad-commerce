namespace SquadCommerce.Agents.Policies;

/// <summary>
/// Immutable policy record that defines operational boundaries for an agent.
/// Enforced by <see cref="PolicyEnforcementFilter"/> in the MAF pipeline.
/// </summary>
public sealed record AgentPolicy
{
    /// <summary>
    /// Name of the agent this policy applies to.
    /// </summary>
    public required string AgentName { get; init; }

    /// <summary>
    /// If true, agent MUST emit A2UI-compliant payloads for complex data.
    /// No raw markdown tables allowed for inventory, pricing, or comparison data.
    /// </summary>
    public required bool EnforceA2UI { get; init; }

    /// <summary>
    /// If true, agent MUST emit OpenTelemetry trace spans for every action.
    /// Enables Aspire Dashboard auditability and troubleshooting.
    /// </summary>
    public required bool RequireTelemetryTrace { get; init; }

    /// <summary>
    /// Primary protocol this agent uses: "AGUI" (orchestrator), "MCP" (domain agents), or "A2A" (external).
    /// </summary>
    public required string PreferredProtocol { get; init; }

    /// <summary>
    /// Whitelist of MCP tool names this agent is allowed to invoke.
    /// Empty list means the agent cannot call tools directly (orchestrators delegate only).
    /// </summary>
    public required IReadOnlyList<string> AllowedTools { get; init; }

    /// <summary>
    /// Entra ID scope required to execute this agent.
    /// Examples: "SquadCommerce.Orchestrate", "SquadCommerce.Inventory.Read"
    /// </summary>
    public required string EntraIdScope { get; init; }
}
