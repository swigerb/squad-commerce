using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// ProcurementAgent identifies alternative compliant suppliers for non-compliant ones.
/// Returns replacement recommendations for at-risk and non-compliant suppliers.
/// </summary>
/// <remarks>
/// Allowed tools: ["GetAlternativeSuppliers"]
/// Required scope: SquadCommerce.Supplier.Read
/// Protocol: MCP
/// </remarks>
public sealed class ProcurementAgent : IDomainAgent
{
    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<ProcurementAgent> _logger;

    public string AgentName => "ProcurementAgent";

    public ProcurementAgent(
        SquadCommerceDbContext dbContext,
        ILogger<ProcurementAgent> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Identifies alternative suppliers for non-compliant ones in a given category.
    /// </summary>
    public async Task<AgentResult> ExecuteAsync(
        string category,
        string certRequired,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "MCP");
        activity?.SetTag("agent.category", category);
        activity?.SetTag("agent.certification", certRequired);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation("ProcurementAgent executing for Category: {Category}, Certification: {Cert}",
            category, certRequired);

        try
        {
            // Find non-compliant suppliers that need replacement
            var nonCompliantSuppliers = await _dbContext.Suppliers
                .Where(s => s.Category == category && (s.Status == "AtRisk" || s.Status == "NonCompliant"))
                .ToListAsync(cancellationToken);

            // Find compliant alternatives
            var compliantAlternatives = await _dbContext.Suppliers
                .Where(s => s.Category == category
                         && s.Certification == certRequired
                         && s.Status == "Compliant")
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);

            if (nonCompliantSuppliers.Count == 0)
            {
                _logger.LogInformation("No non-compliant suppliers to replace for category {Category}", category);

                var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                    new KeyValuePair<string, object?>("agent.name", AgentName));

                return new AgentResult
                {
                    TextSummary = $"No non-compliant {category} suppliers to replace. Supply chain is fully compliant for {certRequired}.",
                    Success = true,
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            var alternativeNames = compliantAlternatives.Select(s => s.Name).ToList();

            var recommendations = nonCompliantSuppliers.Select(s =>
            {
                var replacements = compliantAlternatives.Count > 0
                    ? string.Join(", ", alternativeNames.Take(3))
                    : "No alternatives available";
                return $"• Replace {s.Name} ({s.Country}, {s.Status}) → Alternatives: {replacements}";
            });

            var textSummary = $"Procurement for {category} ({certRequired}): " +
                              $"{nonCompliantSuppliers.Count} supplier(s) need replacement, " +
                              $"{compliantAlternatives.Count} compliant alternative(s) available.\n" +
                              string.Join("\n", recommendations);

            _logger.LogInformation("ProcurementAgent completed: {NonCompliant} to replace, {Alternatives} alternatives",
                nonCompliantSuppliers.Count, compliantAlternatives.Count);

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
            _logger.LogError(ex, "ProcurementAgent failed for Category {Category}", category);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = $"Error finding alternative suppliers for {category}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
