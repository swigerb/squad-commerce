using Microsoft.Extensions.DependencyInjection;
using SquadCommerce.A2A.Validation;
using SquadCommerce.Agents.Domain;
using SquadCommerce.Agents.Orchestrator;
using SquadCommerce.Agents.Orchestrator.Executors;
using SquadCommerce.Agents.Policies;

namespace SquadCommerce.Agents.Registration;

/// <summary>
/// Extension methods for registering Squad-Commerce agents and policies.
/// </summary>
public static class AgentServiceExtensions
{
    /// <summary>
    /// Registers all Squad-Commerce agents, policies, executors, and workflow.
    /// Call this after AddSquadCommerceMcp() to ensure MCP infrastructure is available.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSquadCommerceAgents(this IServiceCollection services)
    {
        // Register A2A validation infrastructure
        services.AddScoped<ExternalDataValidator>();

        // Register policy enforcement infrastructure
        services.AddSingleton<PolicyEnforcementFilter>();

        // Register orchestrator
        services.AddScoped<ChiefSoftwareArchitectAgent>();

        // Register domain agents
        services.AddScoped<InventoryAgent>();
        services.AddScoped<PricingAgent>();
        services.AddScoped<MarketIntelAgent>();

        // Register MAF executor wrappers
        services.AddScoped<MarketIntelExecutor>();
        services.AddScoped<InventoryExecutor>();
        services.AddScoped<PricingExecutor>();
        services.AddScoped<SynthesisExecutor>();

        // Register MAF workflow (depends on executors)
        services.AddScoped<RetailWorkflow>();

        return services;
    }
}
