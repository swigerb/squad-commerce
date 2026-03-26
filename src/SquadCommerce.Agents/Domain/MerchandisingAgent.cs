using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// MerchandisingAgent suggests planogram changes based on traffic patterns
/// and builds an InteractiveFloorplanData A2UI payload with optimization suggestions.
/// </summary>
/// <remarks>
/// Allowed tools: ["GetPlanogramData"]
/// Required scope: SquadCommerce.StoreLayout.Read
/// Protocol: MCP
/// </remarks>
public sealed class MerchandisingAgent : IDomainAgent
{
    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<MerchandisingAgent> _logger;

    public string AgentName => "MerchandisingAgent";

    public MerchandisingAgent(
        SquadCommerceDbContext dbContext,
        ILogger<MerchandisingAgent> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Suggests planogram changes based on traffic patterns and builds optimization A2UI payload.
    /// </summary>
    public async Task<AgentResult> ExecuteAsync(
        string storeId,
        string section,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.store_id", storeId);
        activity?.SetTag("agent.section", section);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation("MerchandisingAgent executing for StoreId: {StoreId}, Section: {Section}",
            storeId, section);

        try
        {
            var allSections = await _dbContext.StoreLayouts
                .Where(sl => sl.StoreId == storeId)
                .ToListAsync(cancellationToken);

            if (allSections.Count == 0)
            {
                _logger.LogWarning("No layout data found for store {StoreId}", storeId);
                return new AgentResult
                {
                    TextSummary = $"No layout data found for store {storeId}",
                    Success = false,
                    ErrorMessage = $"Store {storeId} not found in layout system",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            var maxTraffic = allSections.Max(s => s.AvgHourlyTraffic);
            var storeName = allSections[0].StoreName;

            var floorplanSections = allSections.Select(s =>
            {
                var trafficIntensity = maxTraffic > 0 ? s.AvgHourlyTraffic / maxTraffic : 0.0;
                // Merchandising logic: high-traffic sections should use Front/EndCap placement
                var suggestedPlacement = trafficIntensity switch
                {
                    >= 0.8 => "Front",
                    >= 0.5 => "EndCap",
                    >= 0.3 => "Middle",
                    _ => "Back"
                };
                var isOptimal = string.Equals(s.OptimalPlacement, suggestedPlacement, StringComparison.OrdinalIgnoreCase);

                return new FloorplanSection
                {
                    SectionName = s.Section,
                    SquareFootage = s.SquareFootage,
                    ShelfCount = s.ShelfCount,
                    AvgHourlyTraffic = s.AvgHourlyTraffic,
                    CurrentPlacement = s.OptimalPlacement,
                    SuggestedPlacement = suggestedPlacement,
                    TrafficIntensity = Math.Round(trafficIntensity, 2),
                    OptimizationStatus = isOptimal ? "Optimal" :
                        trafficIntensity >= 0.7 ? "Critical" : "NeedsAdjustment"
                };
            }).ToList();

            var a2uiPayload = new InteractiveFloorplanData
            {
                StoreId = storeId,
                StoreName = storeName,
                Sections = floorplanSections,
                FocusSection = section,
                OpeningDate = DateTimeOffset.UtcNow.AddDays(30),
                Timestamp = DateTimeOffset.UtcNow
            };

            var needsAdjustment = floorplanSections.Count(s => s.OptimizationStatus != "Optimal");
            var criticalCount = floorplanSections.Count(s => s.OptimizationStatus == "Critical");
            var focusSection = floorplanSections.FirstOrDefault(s => s.SectionName == section);

            var textSummary = $"Merchandising analysis for {storeId} ({storeName}): " +
                              $"{needsAdjustment} section(s) need planogram adjustments, {criticalCount} critical. " +
                              $"Focus section '{section}' is {focusSection?.OptimizationStatus ?? "unknown"} — " +
                              $"current placement: {focusSection?.CurrentPlacement ?? "N/A"}, suggested: {focusSection?.SuggestedPlacement ?? "N/A"}.";

            _logger.LogInformation("MerchandisingAgent completed: {NeedsAdjustment} adjustments needed, {Critical} critical",
                needsAdjustment, criticalCount);

            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "InteractiveFloorplan"));

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = textSummary,
                A2UIPayload = a2uiPayload,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MerchandisingAgent failed for StoreId {StoreId}", storeId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = $"Error analyzing merchandising for store {storeId}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
