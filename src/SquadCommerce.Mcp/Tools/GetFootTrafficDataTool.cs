using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for querying foot traffic data from store layouts.
/// Exposed to agents via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Returns traffic heatmap data by section for a given store
/// - Filters by storeId and optional section
/// - Returns structured errors on failure
/// </remarks>
[McpServerToolType]
public sealed class GetFootTrafficDataTool
{
    private const string ToolName = "GetFootTrafficData";

    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<GetFootTrafficDataTool> _logger;

    public GetFootTrafficDataTool(
        SquadCommerceDbContext dbContext,
        ILogger<GetFootTrafficDataTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Queries foot traffic data for a store. Accepts required 'storeId' and optional 'section' parameters.
    /// </summary>
    [McpServerTool(Name = "GetFootTrafficData"), Description("Queries foot traffic heatmap data for a store by section. Returns traffic intensity, shelf counts, and optimal placement data.")]
    public async Task<object> ExecuteAsync(
        [Description("Store ID to query (e.g. SEA-001)")] string storeId,
        [Description("Optional: Filter by section (e.g. Electronics, Grocery, Apparel, Home)")] string? section = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        var parameters = new { storeId, section };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);
        activity?.SetTag("mcp.store_id", storeId);

        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            if (string.IsNullOrWhiteSpace(storeId))
            {
                _logger.LogWarning("GetFootTrafficData called without storeId");
                var valDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(valDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                return new { Success = false, Error = "storeId is required" };
            }

            _logger.LogInformation("GetFootTrafficData executing for storeId={StoreId}, section={Section}",
                storeId, section ?? "(all)");

            var query = _dbContext.StoreLayouts.Where(sl => sl.StoreId == storeId);

            if (!string.IsNullOrWhiteSpace(section))
                query = query.Where(sl => sl.Section == section);

            var layoutData = await query.ToListAsync(cancellationToken);

            if (layoutData.Count == 0)
            {
                _logger.LogWarning("No layout data found for storeId={StoreId}, section={Section}",
                    storeId, section ?? "(all)");

                var emptyDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(emptyDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

                activity?.SetTag("mcp.result.count", 0);

                return new
                {
                    Success = true,
                    StoreId = storeId,
                    Sections = Array.Empty<object>(),
                    Message = $"No layout data found for store {storeId}"
                };
            }

            _logger.LogInformation("Found {Count} layout records for storeId={StoreId}", layoutData.Count, storeId);

            var maxTraffic = layoutData.Max(s => s.AvgHourlyTraffic);

            var resultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(resultDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            activity?.SetTag("mcp.result.count", layoutData.Count);

            return new
            {
                Success = true,
                StoreId = storeId,
                StoreName = layoutData[0].StoreName,
                Sections = layoutData.Select(s => new
                {
                    s.Section,
                    s.SquareFootage,
                    s.ShelfCount,
                    s.AvgHourlyTraffic,
                    s.OptimalPlacement,
                    TrafficIntensity = maxTraffic > 0 ? Math.Round(s.AvgHourlyTraffic / maxTraffic, 2) : 0.0
                }).ToArray(),
                TotalSquareFootage = layoutData.Sum(s => s.SquareFootage),
                AverageTraffic = Math.Round(layoutData.Average(s => s.AvgHourlyTraffic), 1),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetFootTrafficData");

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
