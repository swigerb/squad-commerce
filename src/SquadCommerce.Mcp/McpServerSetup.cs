using Microsoft.Extensions.DependencyInjection;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp;

/// <summary>
/// Extension methods for registering MCP server, tools, and repositories.
/// </summary>
public static class McpServerSetup
{
    /// <summary>
    /// Registers MCP infrastructure, tools, and repositories.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSquadCommerceMcp(this IServiceCollection services)
    {
        // Register repositories (in-memory for demo)
        services.AddSingleton<IInventoryRepository, InventoryRepository>();
        services.AddSingleton<IPricingRepository, PricingRepository>();

        // Register MCP tools
        services.AddSingleton<GetInventoryLevelsTool>();
        services.AddSingleton<UpdateStorePricingTool>();

        // TODO: Register MCP server when ModelContextProtocol package is available
        // services.AddMcpServer(options =>
        // {
        //     options.AddTool<GetInventoryLevelsTool>();
        //     options.AddTool<UpdateStorePricingTool>();
        //     options.UseOpenTelemetry();
        // });

        return services;
    }
}
