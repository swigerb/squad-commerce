using System.ComponentModel;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Mcp.Tools;

/// <summary>
/// MCP tool for querying social media sentiment data.
/// Exposed to agents via the Model Context Protocol using the official ModelContextProtocol SDK.
/// </summary>
/// <remarks>
/// This tool:
/// - Returns structured sentiment data (score, velocity, platform, trend direction)
/// - Filters by SKU, platform, and/or region
/// - Validates parameters and returns structured errors
/// </remarks>
[McpServerToolType]
public sealed class GetSocialSentimentTool
{
    private const string ToolName = "GetSocialSentiment";

    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<GetSocialSentimentTool> _logger;

    public GetSocialSentimentTool(
        SquadCommerceDbContext dbContext,
        ILogger<GetSocialSentimentTool> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Queries social media sentiment data. Accepts optional 'sku', 'platform', and 'region' parameters.
    /// </summary>
    [McpServerTool(Name = "GetSocialSentiment"), Description("Queries social media sentiment data for products. Returns sentiment scores, velocity, and trend direction.")]
    public async Task<object> ExecuteAsync(
        [Description("Optional: Filter by product SKU (e.g. SKU-3001)")] string? sku = null,
        [Description("Optional: Filter by platform (e.g. TikTok, Instagram, Twitter)")] string? platform = null,
        [Description("Optional: Filter by region (e.g. Northeast, Southeast)")] string? region = null,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        var parameters = new { sku, platform, region };
        using var activity = SquadCommerceTelemetry.StartToolSpan(ToolName, parameters);
        activity?.SetTag("mcp.tool.name", ToolName);

        SquadCommerceTelemetry.McpToolCallCount.Add(1,
            new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

        try
        {
            _logger.LogInformation(
                "GetSocialSentiment executing with sku={Sku}, platform={Platform}, region={Region}",
                sku ?? "(all)", platform ?? "(all)", region ?? "(all)");

            var query = _dbContext.SocialSentiment.AsQueryable();

            if (!string.IsNullOrWhiteSpace(sku))
                query = query.Where(s => s.Sku == sku);

            if (!string.IsNullOrWhiteSpace(platform))
                query = query.Where(s => s.Platform == platform);

            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(s => s.Region == region);

            var sentimentData = await query.OrderByDescending(s => s.DetectedAt).ToListAsync(cancellationToken);

            if (sentimentData.Count == 0)
            {
                _logger.LogWarning("No sentiment data found for sku={Sku}, platform={Platform}, region={Region}",
                    sku ?? "(all)", platform ?? "(all)", region ?? "(all)");

                var emptyDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.McpToolCallDuration.Record(emptyDuration,
                    new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

                activity?.SetTag("mcp.result.count", 0);

                return new
                {
                    Success = true,
                    Sku = sku,
                    DataPoints = Array.Empty<object>(),
                    Message = "No sentiment data found matching the specified filters"
                };
            }

            _logger.LogInformation("Found {Count} sentiment records", sentimentData.Count);

            var avgVelocity = sentimentData.Average(s => s.Velocity);
            var trendDirection = avgVelocity > 3.0 ? "surging" :
                                 avgVelocity > 1.5 ? "rising" :
                                 avgVelocity > 0 ? "stable" : "declining";

            var resultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.McpToolCallDuration.Record(resultDuration,
                new KeyValuePair<string, object?>("mcp.tool.name", ToolName));

            activity?.SetTag("mcp.result.count", sentimentData.Count);

            return new
            {
                Success = true,
                Sku = sku,
                TrendDirection = trendDirection,
                AverageVelocity = Math.Round(avgVelocity, 2),
                AverageSentimentScore = Math.Round(sentimentData.Average(s => s.SentimentScore), 3),
                DataPoints = sentimentData.Select(s => new
                {
                    s.Platform,
                    s.SentimentScore,
                    s.Velocity,
                    s.Region,
                    s.ProductName,
                    MeasuredAt = s.DetectedAt
                }).ToArray(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing GetSocialSentiment");

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
