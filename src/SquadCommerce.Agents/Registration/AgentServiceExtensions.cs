using Microsoft.Extensions.DependencyInjection;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Agents.Policies;

namespace SquadCommerce.Agents.Registration;

/// <summary>
/// Extension methods for registering Squad-Commerce agents and policies.
/// </summary>
public static class AgentServiceExtensions
{
    /// <summary>
    /// Registers all Squad-Commerce agents, policies, and MAF infrastructure.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSquadCommerceAgents(this IServiceCollection services)
    {
        // Register policy enforcement infrastructure
        services.AddSingleton<PolicyEnforcementFilter>();

        // Register orchestrator
        services.AddScoped<ChiefSoftwareArchitectAgent>();

        // Register domain agents
        services.AddScoped<InventoryAgent>();
        services.AddScoped<PricingAgent>();
        services.AddScoped<MarketIntelAgent>();

        // Register workflow definitions
        services.AddSingleton<RetailWorkflow>();

        // TODO: Register MAF infrastructure when packages are available
        // services.AddMicrosoftAgentFramework(options =>
        // {
        //     options.UseGraphBasedWorkflow();
        //     options.UseOpenTelemetry();
        //     options.UsePolicyEnforcement<PolicyEnforcementFilter>();
        // });

        // TODO: Register agent policies with MAF runtime
        // foreach (var policy in AgentPolicyRegistry.GetAllPolicies())
        // {
        //     services.Configure<AgentRuntimeOptions>(opts =>
        //         opts.RegisterPolicy(policy.AgentName, policy));
        // }

        return services;
    }
}
