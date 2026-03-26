using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for querying suppliers flagged on sustainability watchlists.
/// Exposed to agents via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Returns suppliers with Status "AtRisk" or "NonCompliant"
/// - Includes watchlist notes and violation details
/// - Filters by optional category
/// </remarks>
[McpServerToolType]
public sealed class GetSustainabilityWatchlistTool
{
    private const string ToolName = "GetSustainabilityWatchlist";

    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<GetSustainabilityWatchlistTool> _logger;

    public GetSustainabilityWatchlistTool(
        SquadCommerceDbContext dbContext,
        ILogger<GetSustainabilityWatchlistTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Queries suppliers flagged on sustainability watchlists (AtRisk or NonCompliant).
    /// </summary>
    [McpServerTool(Name = "GetSustainabilityWatchlist"), Description("Returns suppliers flagged on sustainability watchlists (AtRisk or NonCompliant status). Includes watchlist notes and violation details.")]
    public async Task<object> ExecuteAsync(
        [Description("Optional: Filter by product category (e.g. Cocoa, Coffee, Apparel)")] string? category = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        var parameters = new { category };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);

        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            _logger.LogInformation("GetSustainabilityWatchlist executing with category={Category}",
                category ?? "(all)");

            var query = _dbContext.Suppliers
                .Where(s => s.Status == "AtRisk" || s.Status == "NonCompliant");

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(s => s.Category == category);

            var flaggedSuppliers = await query.OrderBy(s => s.Status).ThenBy(s => s.Name)
                .ToListAsync(cancellationToken);

            if (flaggedSuppliers.Count == 0)
            {
                _logger.LogInformation("No flagged suppliers found for category={Category}", category ?? "(all)");

                var emptyDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(emptyDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

                activity?.SetTag("mcp.result.count", 0);

                return new
                {
                    Success = true,
                    FlaggedSuppliers = Array.Empty<object>(),
                    Message = "No suppliers currently flagged on sustainability watchlists"
                };
            }

            _logger.LogInformation("Found {Count} flagged suppliers", flaggedSuppliers.Count);

            var resultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(resultDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            activity?.SetTag("mcp.result.count", flaggedSuppliers.Count);

            return new
            {
                Success = true,
                Category = category,
                FlaggedSuppliers = flaggedSuppliers.Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.Category,
                    s.Country,
                    s.Certification,
                    CertificationExpiry = s.CertificationExpiry,
                    s.Status,
                    s.WatchlistNotes,
                    DaysUntilExpiry = (int)(s.CertificationExpiry - DateTimeOffset.UtcNow).TotalDays
                }).ToArray(),
                TotalAtRisk = flaggedSuppliers.Count(s => s.Status == "AtRisk"),
                TotalNonCompliant = flaggedSuppliers.Count(s => s.Status == "NonCompliant"),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetSustainabilityWatchlist");

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
