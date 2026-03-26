using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for querying planogram (shelf layout) data and generating optimization suggestions.
/// Exposed to agents via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Returns current shelf layout with optimization suggestions
/// - Compares current placement to optimal placement based on traffic patterns
/// - Requires storeId and section parameters
/// </remarks>
[McpServerToolType]
public sealed class GetPlanogramDataTool
{
    private const string ToolName = "GetPlanogramData";

    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<GetPlanogramDataTool> _logger;

    public GetPlanogramDataTool(
        SquadCommerceDbContext dbContext,
        ILogger<GetPlanogramDataTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Queries planogram data for a store section with optimization suggestions.
    /// </summary>
    [McpServerTool(Name = "GetPlanogramData"), Description("Queries current shelf layout for a store section and compares to optimal placement based on traffic patterns. Returns optimization suggestions.")]
    public async Task<object> ExecuteAsync(
        [Description("Store ID to query (e.g. SEA-001)")] string storeId,
        [Description("Store section to analyze (e.g. Electronics, Grocery, Apparel, Home)")] string section,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        var parameters = new { storeId, section };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);
        activity?.SetTag("mcp.store_id", storeId);
        activity?.SetTag("mcp.section", section);

        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            if (string.IsNullOrWhiteSpace(storeId) || string.IsNullOrWhiteSpace(section))
            {
                _logger.LogWarning("GetPlanogramData called without required parameters");
                var valDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(valDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                return new { Success = false, Error = "Both storeId and section are required" };
            }

            _logger.LogInformation("GetPlanogramData executing for storeId={StoreId}, section={Section}",
                storeId, section);

            var sectionData = await _dbContext.StoreLayouts
                .FirstOrDefaultAsync(sl => sl.StoreId == storeId && sl.Section == section, cancellationToken);

            if (sectionData is null)
            {
                _logger.LogWarning("No planogram data found for storeId={StoreId}, section={Section}",
                    storeId, section);

                var emptyDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(emptyDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

                activity?.SetTag("mcp.result.found", false);

                return new
                {
                    Success = false,
                    Error = $"No planogram data found for store {storeId} section {section}",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // Get all sections for this store to compute relative traffic
            var allSections = await _dbContext.StoreLayouts
                .Where(sl => sl.StoreId == storeId)
                .ToListAsync(cancellationToken);

            var maxTraffic = allSections.Max(s => s.AvgHourlyTraffic);
            var trafficIntensity = maxTraffic > 0 ? sectionData.AvgHourlyTraffic / maxTraffic : 0.0;

            // Determine suggested placement based on traffic intensity
            var suggestedPlacement = trafficIntensity switch
            {
                >= 0.8 => "Front",
                >= 0.5 => "EndCap",
                >= 0.3 => "Middle",
                _ => "Back"
            };

            var isOptimal = string.Equals(sectionData.OptimalPlacement, suggestedPlacement, StringComparison.OrdinalIgnoreCase);
            var optimizationStatus = isOptimal ? "Optimal" :
                trafficIntensity >= 0.7 && sectionData.OptimalPlacement != "Front" ? "Critical" : "NeedsAdjustment";

            var suggestions = new List<string>();
            if (!isOptimal)
            {
                suggestions.Add($"Move high-traffic items from {sectionData.OptimalPlacement} to {suggestedPlacement} placement");
                if (trafficIntensity >= 0.7)
                    suggestions.Add("Consider adding end-cap displays for impulse purchases");
                if (sectionData.ShelfCount < 5)
                    suggestions.Add("Increase shelf density — current layout is underutilizing available square footage");
            }
            suggestions.Add($"Current traffic intensity: {trafficIntensity:P0} of store peak");

            _logger.LogInformation(
                "GetPlanogramData completed: storeId={StoreId}, section={Section}, optimal={IsOptimal}",
                storeId, section, isOptimal);

            var resultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(resultDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            activity?.SetTag("mcp.result.found", true);
            activity?.SetTag("mcp.result.optimal", isOptimal);

            return new
            {
                Success = true,
                StoreId = storeId,
                StoreName = sectionData.StoreName,
                Section = section,
                SquareFootage = sectionData.SquareFootage,
                ShelfCount = sectionData.ShelfCount,
                AvgHourlyTraffic = sectionData.AvgHourlyTraffic,
                CurrentPlacement = sectionData.OptimalPlacement,
                SuggestedPlacement = suggestedPlacement,
                TrafficIntensity = Math.Round(trafficIntensity, 2),
                OptimizationStatus = optimizationStatus,
                Suggestions = suggestions.ToArray(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetPlanogramData");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var errorDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(errorDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            return new
            {
                Success = false,
                Error = $"Internal error: {ex.Message}",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
