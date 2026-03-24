namespace SquadCommerce.Agents.Policies;

/// <summary>
/// Central registry for all agent policies in Squad-Commerce.
/// Policies are registered at startup and enforced by the MAF runtime.
/// </summary>
public static class AgentPolicyRegistry
{
    /// <summary>
    /// Gets all registered agent policies.
    /// </summary>
    public static IReadOnlyList<AgentPolicy> GetAllPolicies() => new[]
    {
        // Orchestrator: No direct tool access, delegates to domain agents
        new AgentPolicy
        {
            AgentName = "ChiefSoftwareArchitect",
            EnforceA2UI = true,
            RequireTelemetryTrace = true,
            PreferredProtocol = "AGUI",
            AllowedTools = Array.Empty<string>(),
            EntraIdScope = "SquadCommerce.Orchestrate"
        },

        // Domain agent: Read-only inventory access
        new AgentPolicy
        {
            AgentName = "InventoryAgent",
            EnforceA2UI = true,
            RequireTelemetryTrace = true,
            PreferredProtocol = "MCP",
            AllowedTools = new[] { "GetInventoryLevels" },
            EntraIdScope = "SquadCommerce.Inventory.Read"
        },

        // Domain agent: Read/write pricing access
        new AgentPolicy
        {
            AgentName = "PricingAgent",
            EnforceA2UI = true,
            RequireTelemetryTrace = true,
            PreferredProtocol = "MCP",
            AllowedTools = new[] { "GetInventoryLevels", "UpdateStorePricing" },
            EntraIdScope = "SquadCommerce.Pricing.ReadWrite"
        },

        // Domain agent: External A2A calls for competitor intel
        new AgentPolicy
        {
            AgentName = "MarketIntelAgent",
            EnforceA2UI = true,
            RequireTelemetryTrace = true,
            PreferredProtocol = "A2A",
            AllowedTools = Array.Empty<string>(), // Uses A2A client instead
            EntraIdScope = "SquadCommerce.MarketIntel.Read"
        }
    };

    /// <summary>
    /// Gets the policy for a specific agent by name.
    /// </summary>
    public static AgentPolicy? GetPolicyByName(string agentName) =>
        GetAllPolicies().FirstOrDefault(p => p.AgentName.Equals(agentName, StringComparison.OrdinalIgnoreCase));
}
