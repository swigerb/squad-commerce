using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for finding compliant alternative suppliers that could replace at-risk ones.
/// Exposed to agents via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Returns compliant suppliers matching category and certification requirements
/// - Used by ProcurementAgent to find replacements for non-compliant suppliers
/// - Requires both category and certification parameters
/// </remarks>
[McpServerToolType]
public sealed class GetAlternativeSuppliersTool
{
    private const string ToolName = "GetAlternativeSuppliers";

    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<GetAlternativeSuppliersTool> _logger;

    public GetAlternativeSuppliersTool(
        SquadCommerceDbContext dbContext,
        ILogger<GetAlternativeSuppliersTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Finds compliant alternative suppliers for a given category and certification requirement.
    /// </summary>
    [McpServerTool(Name = "GetAlternativeSuppliers"), Description("Finds compliant alternative suppliers that could replace at-risk or non-compliant ones. Requires product category and certification type.")]
    public async Task<object> ExecuteAsync(
        [Description("Product category to search (e.g. Cocoa, Coffee, Apparel)")] string category,
        [Description("Required certification type (e.g. FairTrade, Organic, RainforestAlliance)")] string certification,
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
            if (string.IsNullOrWhiteSpace(category) || string.IsNullOrWhiteSpace(certification))
            {
                _logger.LogWarning("GetAlternativeSuppliers called without required parameters");
                var valDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(valDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                return new { Success = false, Error = "Both category and certification are required" };
            }

            _logger.LogInformation("GetAlternativeSuppliers executing for category={Category}, certification={Certification}",
                category, certification);

            var compliantSuppliers = await _dbContext.Suppliers
                .Where(s => s.Category == category
                         && s.Certification == certification
                         && s.Status == "Compliant")
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            if (compliantSuppliers.Count == 0)
            {
                _logger.LogWarning("No compliant alternatives found for category={Category}, certification={Certification}",
                    category, certification);

                var emptyDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(emptyDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

                activity?.SetTag("mcp.result.count", 0);

                return new
                {
                    Success = true,
                    AlternativeSuppliers = Array.Empty<object>(),
                    Message = $"No compliant {certification}-certified {category} suppliers found"
                };
            }

            _logger.LogInformation("Found {Count} compliant alternative suppliers", compliantSuppliers.Count);

            var resultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(resultDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            activity?.SetTag("mcp.result.count", compliantSuppliers.Count);

            return new
            {
                Success = true,
                Category = category,
                Certification = certification,
                AlternativeSuppliers = compliantSuppliers.Select(s => new
                {
                    s.SupplierId,
                    s.Name,
                    s.Country,
                    s.Certification,
                    CertificationExpiry = s.CertificationExpiry,
                    DaysUntilExpiry = (int)(s.CertificationExpiry - DateTimeOffset.UtcNow).TotalDays
                }).ToArray(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetAlternativeSuppliers");

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
