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
        services.AddScoped<MarketingAgent>();

        // Register MAF executor wrappers (Competitor Price Drop workflow)
        services.AddScoped<MarketIntelExecutor>();
        services.AddScoped<InventoryExecutor>();
        services.AddScoped<PricingExecutor>();
        services.AddScoped<SynthesisExecutor>();

        // Register MAF executor wrappers (Viral Spike workflow)
        services.AddScoped<ViralSpikeSentimentExecutor>();
        services.AddScoped<ViralSpikePricingExecutor>();
        services.AddScoped<ViralSpikeMarketingExecutor>();
        services.AddScoped<ViralSpikeSynthesisExecutor>();

        // Register domain agents (Supply Chain Shock)
        services.AddScoped<LogisticsAgent>();
        services.AddScoped<RedistributionAgent>();

        // Register domain agents (Store Readiness)
        services.AddScoped<TrafficAnalystAgent>();
        services.AddScoped<MerchandisingAgent>();
        services.AddScoped<ManagerAgent>();

        // Register domain agents (ESG Audit)
        services.AddScoped<ComplianceAgent>();
        services.AddScoped<ResearchAgent>();
        services.AddScoped<ProcurementAgent>();

        // Register MAF executor wrappers (Supply Chain Shock workflow)
        services.AddScoped<LogisticsExecutor>();
        services.AddScoped<SupplyChainInventoryExecutor>();
        services.AddScoped<RedistributionExecutor>();
        services.AddScoped<SupplyChainSynthesisExecutor>();

        // Register MAF executor wrappers (Store Readiness workflow)
        services.AddScoped<TrafficExecutor>();
        services.AddScoped<MerchandisingExecutor>();
        services.AddScoped<ManagerHitlExecutor>();
        services.AddScoped<StoreReadinessSynthesisExecutor>();

        // Register MAF executor wrappers (ESG Audit workflow)
        services.AddScoped<ComplianceExecutor>();
        services.AddScoped<ResearchExecutor>();
        services.AddScoped<ProcurementExecutor>();
        services.AddScoped<ESGSynthesisExecutor>();

        // Register MAF workflows (depend on executors)
        services.AddScoped<RetailWorkflow>();
        services.AddScoped<ViralSpikeWorkflow>();
        services.AddScoped<SupplyChainWorkflow>();
        services.AddScoped<StoreReadinessWorkflow>();
        services.AddScoped<ESGAuditWorkflow>();

        return services;
    }
}
