using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SquadCommerce.Observability;

namespace SquadCommerce.Agents.Domain;

/// <summary>
/// ManagerAgent is the HITL (Human-in-the-Loop) agent that reviews and approves
/// merchandising recommendations. For demo: auto-approves with a simulated delay.
/// In future: would wire to SignalR for real human approval.
/// </summary>
/// <remarks>
/// Allowed tools: []
/// Required scope: SquadCommerce.Manager.Approve
/// Protocol: Internal (HITL)
/// </remarks>
public sealed class ManagerAgent : IDomainAgent
{
    private readonly ILogger<ManagerAgent> _logger;

    public string AgentName => "ManagerAgent";

    public ManagerAgent(ILogger<ManagerAgent> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reviews merchandising recommendations and auto-approves with simulated delay.
    /// </summary>
    /// <param name="storeId">Store being reviewed</param>
    /// <param name="section">Section being reviewed</param>
    /// <param name="merchandisingResult">Result from MerchandisingAgent to review</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent result with approval status</returns>
    public async Task<AgentResult> ExecuteAsync(
        string storeId,
        string section,
        AgentResult merchandisingResult,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTimeOffset.UtcNow;

        using var activity = SquadCommerceTelemetry.StartAgentSpan(AgentName, "Execute");
        activity?.SetTag("agent.name", AgentName);
        activity?.SetTag("agent.protocol", "Internal");
        activity?.SetTag("agent.store_id", storeId);
        activity?.SetTag("agent.section", section);
        activity?.SetTag("agent.hitl", true);

        SquadCommerceTelemetry.AgentInvocationCount.Add(1,
            new KeyValuePair<string, object?>("agent.name", AgentName));

        _logger.LogInformation("ManagerAgent reviewing merchandising recommendations for StoreId: {StoreId}, Section: {Section}",
            storeId, section);

        try
        {
            // Simulate manager review delay (500ms)
            await Task.Delay(500, cancellationToken);

            var approved = merchandisingResult.Success;
            var managerNotes = approved
                ? $"Approved planogram changes for {section} section at store {storeId}. " +
                  "Recommendations align with traffic patterns and revenue targets. Proceed with implementation."
                : $"Deferred planogram changes for {section} section at store {storeId}. " +
                  "Merchandising analysis returned errors — review required before approval.";

            _logger.LogInformation("ManagerAgent {Decision} recommendations for StoreId: {StoreId}, Section: {Section}",
                approved ? "approved" : "deferred", storeId, section);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = managerNotes,
                Success = approved,
                ErrorMessage = approved ? null : "Merchandising analysis had errors — manager deferred approval",
                Timestamp = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ManagerAgent failed for StoreId {StoreId}", storeId);

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.message", ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);

            var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
            SquadCommerceTelemetry.AgentInvocationDuration.Record(duration,
                new KeyValuePair<string, object?>("agent.name", AgentName));

            return new AgentResult
            {
                TextSummary = $"Error during manager review for store {storeId}",
                Success = false,
                ErrorMessage = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }
    }
}
