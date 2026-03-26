using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// ResearchAgent cross-references suppliers against sustainability watchlists.
/// Returns findings with confidence levels for flagged suppliers.
/// </summary>
/// <remarks>
/// Allowed tools: ["GetSustainabilityWatchlist"]
/// Required scope: SquadCommerce.Supplier.Read
/// Protocol: MCP
/// </remarks>
public sealed class ResearchAgent : IDomainAgent
{
    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<ResearchAgent> _logger;

    public string AgentName => "ResearchAgent";

    public ResearchAgent(
        SquadCommerceDbContext dbContext,
        ILogger<ResearchAgent> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Cross-references suppliers against sustainability watchlists and returns findings.
    /// </summary>
    public async Task<AgentResult> ExecuteAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.category", category);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation("ResearchAgent executing for Category: {Category}", category);

        try
        {
            var flaggedSuppliers = await _dbContext.Suppliers
                .Where(s => s.Category == category && (s.Status == "AtRisk" || s.Status == "NonCompliant"))
                .OrderBy(s => s.Status)
                .ThenBy(s => s.Name)
                .ToListAsync(cancellationToken);

            if (flaggedSuppliers.Count == 0)
            {
                _logger.LogInformation("No flagged suppliers found for category {Category}", category);

                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                    new KeyValuePair<string, object?>("agent.name", AgentName));

                return new AgentResult
                {
                    TextSummary = $"No suppliers flagged on sustainability watchlists for {category}. All suppliers compliant.",
                    Success = true,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            // Build detailed findings with confidence levels
            var findings = flaggedSuppliers.Select(s =>
            {
                var confidence = s.Status == "NonCompliant" ? "High" : "Medium";
                var daysUntilExpiry = (int)(s.CertificationExpiry - DateTimeOffset.UtcNow).TotalDays;
                return $"• {s.Name} ({s.Country}): Status={s.Status}, Confidence={confidence}, " +
                       $"Cert expires in {daysUntilExpiry} days" +
                       (s.WatchlistNotes != null ? $" — {s.WatchlistNotes}" : "");
            });

            var nonCompliantCount = flaggedSuppliers.Count(s => s.Status == "NonCompliant");
            var atRiskCount = flaggedSuppliers.Count(s => s.Status == "AtRisk");

            var textSummary = $"Sustainability watchlist for {category}: " +
                              $"{flaggedSuppliers.Count} supplier(s) flagged — " +
                              $"{nonCompliantCount} non-compliant (high confidence), {atRiskCount} at-risk (medium confidence).\n" +
                              string.Join("\n", findings);

            _logger.LogInformation("ResearchAgent completed: {Flagged} flagged suppliers for {Category}",
                flaggedSuppliers.Count, category);

            var resultDuration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(resultDuration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = textSummary,
                Success = true,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResearchAgent failed for Category {Category}", category);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = $"Error researching sustainability watchlist for {category}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
