namespace SquadCommerce.A2A;

/// <summary>
/// Agent Card is the A2A protocol's discovery mechanism.
/// It describes an agent's capabilities, supported protocols, and contact information.
/// </summary>
/// <remarks>
/// Agent Cards are published to a registry (or served at a well-known endpoint)
/// so other agents can discover and communicate with them via A2A.
/// 
/// Spec reference: https://github.com/microsoft/a2a-protocol
/// </remarks>
public sealed record AgentCard
{
    /// <summary>
    /// Unique identifier for this agent (e.g., "com.squadcommerce.inventory")
    /// </summary>
    public required string AgentId { get; init; }

    /// <summary>
    /// Human-readable name (e.g., "Squad-Commerce Inventory Agent")
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Agent description and capabilities
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Protocol version supported (e.g., "1.0")
    /// </summary>
    public required string ProtocolVersion { get; init; }

    /// <summary>
    /// Base URL for A2A requests (e.g., "https://api.squadcommerce.com/a2a")
    /// </summary>
    public required string Endpoint { get; init; }

    /// <summary>
    /// Authentication requirements ("none", "api-key", "oauth2", "mutual-tls")
    /// </summary>
    public required string AuthType { get; init; }

    /// <summary>
    /// List of supported capabilities (e.g., ["GetInventoryLevels", "GetStorePricing"])
    /// </summary>
    public required IReadOnlyList<string> Capabilities { get; init; }

    /// <summary>
    /// Contact information for agent owner
    /// </summary>
    public required ContactInfo Contact { get; init; }
}

/// <summary>
/// Contact information for the agent owner.
/// </summary>
public sealed record ContactInfo(string Name, string Email, string Organization);

/// <summary>
/// Factory for creating Agent Cards for Squad-Commerce agents.
/// </summary>
public static class AgentCardFactory
{
    /// <summary>
    /// Creates an Agent Card for the InventoryAgent.
    /// </summary>
    public static AgentCard CreateInventoryAgentCard(string baseUrl)
    {
        return new AgentCard
        {
            AgentId = "com.squadcommerce.inventory",
            Name = "Squad-Commerce Inventory Agent",
            Description = "Provides real-time inventory levels across retail stores",
            ProtocolVersion = "1.0",
            Endpoint = $"{baseUrl}/a2a/inventory",
            AuthType = "oauth2",
            Capabilities = new[] { "GetInventoryLevels", "GetLowStockAlerts" },
            Contact = new ContactInfo("Squad-Commerce Team", "support@squadcommerce.com", "Squad-Commerce")
        };
    }

    /// <summary>
    /// Creates an Agent Card for the PricingAgent.
    /// </summary>
    public static AgentCard CreatePricingAgentCard(string baseUrl)
    {
        return new AgentCard
        {
            AgentId = "com.squadcommerce.pricing",
            Name = "Squad-Commerce Pricing Agent",
            Description = "Provides current pricing and can calculate margin impact",
            ProtocolVersion = "1.0",
            Endpoint = $"{baseUrl}/a2a/pricing",
            AuthType = "oauth2",
            Capabilities = new[] { "GetStorePricing", "CalculateMarginImpact" },
            Contact = new ContactInfo("Squad-Commerce Team", "support@squadcommerce.com", "Squad-Commerce")
        };
    }
}
