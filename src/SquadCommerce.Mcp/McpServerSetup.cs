using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SquadCommerce.Contracts.Interfaces;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Mcp.Tools;

namespace SquadCommerce.Mcp;

/// <summary>
/// Extension methods for registering MCP server, tools, and repositories.
/// Uses the official ModelContextProtocol SDK for protocol-compliant tool hosting.
/// </summary>
public static class McpServerSetup
{
    /// <summary>
    /// Registers MCP infrastructure, tools, and repositories.
    /// Uses EF Core + SQLite for data persistence and the official ModelContextProtocol SDK
    /// for protocol-compliant tool discovery and invocation.
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
        // These are also discovered by the MCP SDK via [McpServerToolType] attributes
        services.AddScoped<GetInventoryLevelsTool>();
        services.AddScoped<UpdateStorePricingTool>();

        // Register the official MCP server with HTTP transport
        // Tools are auto-discovered from the SquadCommerce.Mcp assembly via [McpServerToolType] attributes
        services.AddMcpServer(options =>
        {
            options.ServerInfo = new()
            {
                Name = "SquadCommerce MCP Server",
                Version = "1.0.0"
            };
        })
        .WithHttpTransport()
        .WithToolsFromAssembly(typeof(GetInventoryLevelsTool).Assembly);

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
