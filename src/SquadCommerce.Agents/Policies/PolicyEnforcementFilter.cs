namespace SquadCommerce.Agents.Policies;

/// <summary>
/// MAF filter that enforces agent policies before execution.
/// Validates tool access, telemetry requirements, and Entra ID scopes.
/// </summary>
/// <remarks>
/// This is a stub implementation. In a real MAF integration, this would:
/// 1. Hook into the MAF middleware pipeline
/// 2. Inspect the AgentExecutionContext for the current agent
/// 3. Validate against the registered AgentPolicy
/// 4. Block execution or log violations as appropriate
/// </remarks>
public sealed class PolicyEnforcementFilter
{
    private readonly IReadOnlyDictionary<string, AgentPolicy> _policies;

    public PolicyEnforcementFilter()
    {
        _policies = AgentPolicyRegistry.GetAllPolicies()
            .ToDictionary(p => p.AgentName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that the requested tool is allowed for the executing agent.
    /// </summary>
    public bool IsToolAllowed(string agentName, string toolName)
    {
        if (!_policies.TryGetValue(agentName, out var policy))
        {
            return false; // Unknown agent - deny by default
        }

        return policy.AllowedTools.Contains(toolName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates that the current user has the required Entra ID scope for the agent.
    /// </summary>
    public bool HasRequiredScope(string agentName, IEnumerable<string> userScopes)
    {
        if (!_policies.TryGetValue(agentName, out var policy))
        {
            return false; // Unknown agent - deny by default
        }

        return userScopes.Contains(policy.EntraIdScope, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the policy for a specific agent.
    /// </summary>
    public AgentPolicy? GetPolicy(string agentName)
    {
        _policies.TryGetValue(agentName, out var policy);
        return policy;
    }
}
