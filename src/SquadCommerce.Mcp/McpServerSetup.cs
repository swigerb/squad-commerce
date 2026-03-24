using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SquadCommerce.Contracts.Interfaces;
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
    /// Uses EF Core + SQLite for data persistence.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSquadCommerceMcp(this IServiceCollection services)
    {
        // Register DbContext with SQLite
        services.AddDbContext<SquadCommerceDbContext>(options =>
            options.UseSqlite("Data Source=squadcommerce.db"));

        // Register database seeder
        services.AddScoped<DatabaseSeeder>();

        // Register audit repository
        services.AddScoped<AuditRepository>();

        // Register repositories with Contracts interfaces (SQLite via EF Core)
        services.AddScoped<IInventoryRepository, SqliteInventoryRepository>();
        services.AddScoped<IPricingRepository, SqlitePricingRepository>();

        // Register MCP tools as scoped (they depend on scoped repositories)
        services.AddScoped<GetInventoryLevelsTool>();
        services.AddScoped<UpdateStorePricingTool>();

        // Register MCP tool registry for discovery (singleton, no dependencies on scoped services)
        services.AddSingleton<IMcpToolRegistry, McpToolRegistry>();

        return services;
    }

    /// <summary>
    /// Ensures the database is created and seeded with demo data.
    /// Call this after building the host/app.
    /// </summary>
    /// <param name="host">The host instance</param>
    /// <returns>The host for chaining</returns>
    public static async Task<IHost> UseSquadCommerceDatabaseAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<SquadCommerceDbContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();

        // Create database and schema
        await context.Database.EnsureCreatedAsync();

        // Seed demo data (idempotent)
        await seeder.SeedAsync();

        return host;
    }
}

/// <summary>
/// MCP tool registry for discovering and invoking tools by name.
/// This abstraction allows us to swap in the real ModelContextProtocol package later.
/// </summary>
public interface IMcpToolRegistry
{
    /// <summary>
    /// Gets the schema for a tool, describing its parameters and return type.
    /// </summary>
    ToolSchema GetToolSchema(string toolName);
    
    /// <summary>
    /// Invokes a tool by name with the provided parameters.
    /// </summary>
    Task<object> InvokeToolAsync(string toolName, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets all registered tool names.
    /// </summary>
    IReadOnlyList<string> GetAllToolNames();
}

/// <summary>
/// Schema describing an MCP tool's parameters and return type.
/// </summary>
public sealed record ToolSchema(
    string Name,
    string Description,
    IReadOnlyList<ToolParameter> Parameters,
    string ReturnType);

/// <summary>
/// Parameter definition for an MCP tool.
/// </summary>
public sealed record ToolParameter(
    string Name,
    string Type,
    string Description,
    bool Required);

/// <summary>
/// Default implementation of MCP tool registry.
/// Uses service provider for scoped tool resolution.
/// </summary>
internal sealed class McpToolRegistry : IMcpToolRegistry
{
    private readonly IServiceProvider _serviceProvider;

    public McpToolRegistry(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public ToolSchema GetToolSchema(string toolName)
    {
        return toolName switch
        {
            "GetInventoryLevels" => new ToolSchema(
                "GetInventoryLevels",
                "Queries inventory levels for stores. Accepts optional 'sku' or 'storeId' parameters.",
                new[]
                {
                    new ToolParameter("sku", "string?", "Optional: Filter by SKU", false),
                    new ToolParameter("storeId", "string?", "Optional: Filter by store ID", false)
                },
                "object"),
            "UpdateStorePricing" => new ToolSchema(
                "UpdateStorePricing",
                "Updates the price of a SKU at a specific store. Requires storeId, sku, and newPrice parameters.",
                new[]
                {
                    new ToolParameter("storeId", "string", "Store ID where price will be updated", true),
                    new ToolParameter("sku", "string", "Product SKU to update", true),
                    new ToolParameter("newPrice", "decimal", "New price to set", true)
                },
                "object"),
            _ => throw new ArgumentException($"Unknown tool: {toolName}", nameof(toolName))
        };
    }

    public async Task<object> InvokeToolAsync(string toolName, IDictionary<string, object?> parameters, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        
        return toolName switch
        {
            "GetInventoryLevels" => await scope.ServiceProvider.GetRequiredService<GetInventoryLevelsTool>().ExecuteAsync(
                parameters.TryGetValue("sku", out var skuVal) ? skuVal?.ToString() : null,
                parameters.TryGetValue("storeId", out var storeIdVal) ? storeIdVal?.ToString() : null,
                cancellationToken),
            "UpdateStorePricing" => await scope.ServiceProvider.GetRequiredService<UpdateStorePricingTool>().ExecuteAsync(
                parameters.TryGetValue("storeId", out var storeIdVal2) && storeIdVal2 != null ? storeIdVal2.ToString() : throw new ArgumentException("storeId is required"),
                parameters.TryGetValue("sku", out var skuVal2) && skuVal2 != null ? skuVal2.ToString() : throw new ArgumentException("sku is required"),
                parameters.TryGetValue("newPrice", out var priceVal) && priceVal != null ? Convert.ToDecimal(priceVal) : throw new ArgumentException("newPrice is required"),
                cancellationToken),
            _ => throw new ArgumentException($"Unknown tool: {toolName}", nameof(toolName))
        };
    }

    public IReadOnlyList<string> GetAllToolNames()
    {
        return new[] { "GetInventoryLevels", "UpdateStorePricing" };
    }
}
