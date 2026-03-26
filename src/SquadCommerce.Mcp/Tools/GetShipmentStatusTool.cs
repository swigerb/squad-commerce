using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for querying shipment status, ETA, and delay information.
/// Exposed to agents via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Queries ShipmentEntity from the database
/// - Returns shipment status, ETA, and delay information
/// - Filters by SKU or ShipmentId if provided
/// - Returns structured errors on failure (never throws)
/// </remarks>
[McpServerToolType]
public sealed class GetShipmentStatusTool
{
    private const string ToolName = "GetShipmentStatus";

    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<GetShipmentStatusTool> _logger;

    public GetShipmentStatusTool(
        SquadCommerceDbContext dbContext,
        ILogger<GetShipmentStatusTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Queries shipment status for a given SKU or shipment ID.
    /// </summary>
    [McpServerTool(Name = "GetShipmentStatus"), Description("Queries shipment status, ETA, and delay information. Filter by SKU or ShipmentId.")]
    public async Task<object> ExecuteAsync(
        [Description("Optional: Filter by product SKU (e.g. SKU-2001)")] string? sku = null,
        [Description("Optional: Filter by shipment ID (e.g. SHP-001)")] string? shipmentId = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        var parameters = new { sku, shipmentId };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);

        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            if (string.IsNullOrWhiteSpace(sku) && string.IsNullOrWhiteSpace(shipmentId))
            {
                _logger.LogWarning("GetShipmentStatus called without sku or shipmentId");
                var valDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(valDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                return new { Success = false, Error = "Please provide either 'sku' or 'shipmentId' parameter" };
            }

            _logger.LogInformation("GetShipmentStatus executing with sku={Sku}, shipmentId={ShipmentId}",
                sku ?? "(all)", shipmentId ?? "(all)");

            var query = _dbContext.Shipments.AsQueryable();

            if (!string.IsNullOrWhiteSpace(shipmentId))
                query = query.Where(s => s.ShipmentId == shipmentId);

            if (!string.IsNullOrWhiteSpace(sku))
                query = query.Where(s => s.Sku == sku);

            var shipments = await query.ToListAsync(cancellationToken);

            if (shipments.Count == 0)
            {
                _logger.LogWarning("No shipments found for sku={Sku}, shipmentId={ShipmentId}", sku, shipmentId);
                var emptyDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(emptyDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
                activity?.SetTag("mcp.result.count", 0);

                return new
                {
                    Success = true,
                    Shipments = Array.Empty<object>(),
                    Message = $"No shipment records found for {(sku != null ? $"SKU {sku}" : $"ShipmentId {shipmentId}")}"
                };
            }

            var result = shipments.Select(s => new
            {
                s.ShipmentId,
                s.Sku,
                s.ProductName,
                s.OriginStoreId,
                s.DestStoreId,
                s.Status,
                s.EstimatedArrival,
                s.DelayDays,
                s.DelayReason,
                IsDelayed = s.Status == "Delayed",
                s.CreatedAt
            }).ToArray();

            var delayedCount = result.Count(s => s.IsDelayed);

            _logger.LogInformation("GetShipmentStatus found {Count} shipments ({Delayed} delayed)",
                result.Length, delayedCount);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(duration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));
            activity?.SetTag("mcp.result.count", result.Length);

            return new
            {
                Success = true,
                Shipments = result,
                TotalCount = result.Length,
                DelayedCount = delayedCount,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetShipmentStatus");

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
