using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for querying supplier certification and compliance data.
/// Exposed to agents via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Returns supplier compliance status, certifications, and expiry dates
/// - Filters by category and/or certification
/// - Returns structured errors on failure
/// </remarks>
[McpServerToolType]
public sealed class GetSupplierCertificationsTool
{
    private const string ToolName = "GetSupplierCertifications";

    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<GetSupplierCertificationsTool> _logger;

    public GetSupplierCertificationsTool(
        SquadCommerceDbContext dbContext,
        ILogger<GetSupplierCertificationsTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Queries supplier certifications. Accepts optional 'category' and 'certification' parameters.
    /// </summary>
    [McpServerTool(Name = "GetSupplierCertifications"), Description("Queries supplier compliance status, certifications, and expiry dates. Filter by product category and/or certification type.")]
    public async Task<object> ExecuteAsync(
        [Description("Optional: Filter by product category (e.g. Cocoa, Coffee, Apparel)")] string? category = null,
        [Description("Optional: Filter by certification type (e.g. FairTrade, Organic, RainforestAlliance)")] string? certification = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        var parameters = new { category, certification };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);

        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            _logger.LogInformation("GetSupplierCertifications executing with category={Category}, certification={Certification}",
                category ?? "(all)", certification ?? "(all)");

            var query = _dbContext.Suppliers.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(s => s.Category == category);

            if (!string.IsNullOrWhiteSpace(certification))
                query = query.Where(s => s.Certification == certification);

            var suppliers = await query.OrderBy(s => s.Name).ToListAsync(cancellationToken);

            if (suppliers.Count == 0)
            {
                _logger.LogWarning("No suppliers found for category={Category}, certification={Certification}",
                    category ?? "(all)", certification ?? "(all)");

                var emptyDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(emptyDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

                activity?.SetTag("mcp.result.count", 0);

                return new
                {
                    Success = true,
                    Suppliers = Array.Empty<object>(),
                    Message = "No suppliers found matching the specified filters"
                };
            }

            _logger.LogInformation("Found {Count} suppliers", suppliers.Count);

            var resultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(resultDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            activity?.SetTag("mcp.result.count", suppliers.Count);

            return new
            {
                Success = true,
                Category = category,
                Certification = certification,
                Suppliers = suppliers.Select(s => new
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
                TotalCompliant = suppliers.Count(s => s.Status == "Compliant"),
                TotalAtRisk = suppliers.Count(s => s.Status == "AtRisk"),
                TotalNonCompliant = suppliers.Count(s => s.Status == "NonCompliant"),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetSupplierCertifications");

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
