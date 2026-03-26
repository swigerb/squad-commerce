using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// ComplianceAgent queries supplier certifications and identifies at-risk and
/// non-compliant suppliers. Builds a SupplierRiskMatrixData A2UI payload.
/// </summary>
/// <remarks>
/// Allowed tools: ["GetSupplierCertifications"]
/// Required scope: SquadCommerce.Supplier.Read
/// Protocol: MCP
/// </remarks>
public sealed class ComplianceAgent : IDomainAgent
{
    private readonly SquadCommerceDbContext _dbContext;
    private readonly ILogger<ComplianceAgent> _logger;

    public string AgentName => "ComplianceAgent";

    public ComplianceAgent(
        SquadCommerceDbContext dbContext,
        ILogger<ComplianceAgent> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Queries supplier certifications, identifies at-risk suppliers, and builds SupplierRiskMatrix A2UI.
    /// </summary>
    public async Task<AgentResult> ExecuteAsync(
        string category,
        string certRequired,
        DateTimeOffset deadline,
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

        _logger.LogInformation("ComplianceAgent executing for Category: {Category}, Certification: {Cert}, Deadline: {Deadline}",
            category, certRequired, deadline);

        try
        {
            var suppliers = await _dbContext.Suppliers
                .Where(s => s.Category == category)
                .OrderBy(s => s.Status)
                .ThenBy(s => s.Name)
                .ToListAsync(cancellationToken);

            if (suppliers.Count == 0)
            {
                _logger.LogWarning("No suppliers found for category {Category}", category);
                return new AgentResult
                {
                    TextSummary = $"No suppliers found for category {category}",
                    Success = false,
                    ErrorMessage = $"Category {category} not found in supplier system",
                    Timestamp = DateTimeOffset.UtcNow
                };
            }

            var riskEntries = suppliers.Select(s => new SupplierRiskEntry
            {
                SupplierId = s.SupplierId,
                SupplierName = s.Name,
                Country = s.Country,
                Certification = s.Certification,
                CertificationExpiry = s.CertificationExpiry,
                RiskLevel = s.Status,
                WatchlistNotes = s.WatchlistNotes
            }).ToList();

            var compliantCount = suppliers.Count(s => s.Status == "Compliant");
            var atRiskCount = suppliers.Count(s => s.Status == "AtRisk");
            var nonCompliantCount = suppliers.Count(s => s.Status == "NonCompliant");

            var a2uiPayload = new SupplierRiskMatrixData
            {
                ProductCategory = category,
                CertificationRequired = certRequired,
                Suppliers = riskEntries,
                TotalCompliant = compliantCount,
                TotalAtRisk = atRiskCount,
                TotalNonCompliant = nonCompliantCount,
                Deadline = deadline,
                Timestamp = DateTimeOffset.UtcNow
            };

            var textSummary = $"ESG compliance for {category} ({certRequired}): " +
                              $"{suppliers.Count} suppliers evaluated — " +
                              $"{compliantCount} compliant, {atRiskCount} at-risk, {nonCompliantCount} non-compliant. " +
                              $"Deadline: {deadline:yyyy-MM-dd}.";

            _logger.LogInformation("ComplianceAgent completed: {Compliant} compliant, {AtRisk} at-risk, {NonCompliant} non-compliant",
                compliantCount, atRiskCount, nonCompliantCount);

            SquadCommerceTelemetry.A2UIPayloadCount.Add(1,
                new KeyValuePair<string, object?>("a2ui.component", "SupplierRiskMatrix"));

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
            _logger.LogError(ex, "ComplianceAgent failed for Category {Category}", category);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = $"Error evaluating supplier compliance for {category}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
