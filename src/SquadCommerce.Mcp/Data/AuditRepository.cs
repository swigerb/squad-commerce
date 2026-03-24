using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data.Entities;

namespace SquadCommerce.Mcp.Data;

/// <summary>
/// Repository for managing audit trail entries.
/// Provides persistence and retrieval of agent actions, decisions, and protocol interactions.
/// </summary>
public sealed class AuditRepository
{
    private readonly SquadCommerceDbContext _context;
    private readonly ILogger<AuditRepository> _logger;

    public AuditRepository(SquadCommerceDbContext context, ILogger<AuditRepository> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Records a new audit entry to the database.
    /// </summary>
    /// <param name="sessionId">Session identifier for grouping</param>
    /// <param name="entry">Audit entry to persist</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RecordAuditEntryAsync(string sessionId, AuditEntry entry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be null or whitespace", nameof(sessionId));
        
        if (entry == null)
            throw new ArgumentNullException(nameof(entry));

        var entity = new AuditEntryEntity
        {
            Id = entry.Id,
            SessionId = sessionId,
            AgentName = entry.AgentName,
            Action = entry.Action,
            Protocol = entry.Protocol,
            Timestamp = entry.Timestamp,
            DurationMs = (long)entry.Duration.TotalMilliseconds,
            Status = entry.Status,
            Details = entry.Details,
            TraceId = entry.TraceId,
            DecisionOutcome = entry.DecisionOutcome,
            AffectedSkusCsv = entry.AffectedSkus != null ? string.Join(",", entry.AffectedSkus) : null,
            AffectedStoresCsv = entry.AffectedStores != null ? string.Join(",", entry.AffectedStores) : null
        };

        _context.AuditEntries.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Recorded audit entry: {AgentName} - {Action} - {Status}",
            entry.AgentName,
            entry.Action,
            entry.Status);
    }

    /// <summary>
    /// Retrieves all audit entries for a specific session, ordered by timestamp.
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit entries for the session</returns>
    public async Task<IReadOnlyList<AuditEntry>> GetAuditTrailAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be null or whitespace", nameof(sessionId));

        var entities = await _context.AuditEntries
            .Where(e => e.SessionId == sessionId)
            .OrderBy(e => e.Timestamp)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Retrieved {Count} audit entries for session {SessionId}",
            entities.Count,
            sessionId);

        return entities.Select(MapToAuditEntry).ToList();
    }

    /// <summary>
    /// Retrieves the most recent audit entries across all sessions.
    /// </summary>
    /// <param name="count">Maximum number of entries to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of recent audit entries</returns>
    public async Task<IReadOnlyList<AuditEntry>> GetRecentAuditEntriesAsync(
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be greater than 0");

        var entities = await _context.AuditEntries
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} recent audit entries", entities.Count);

        return entities.Select(MapToAuditEntry).ToList();
    }

    private static AuditEntry MapToAuditEntry(AuditEntryEntity entity)
    {
        return new AuditEntry
        {
            Id = entity.Id,
            AgentName = entity.AgentName,
            Action = entity.Action,
            Protocol = entity.Protocol,
            Timestamp = entity.Timestamp,
            Duration = TimeSpan.FromMilliseconds(entity.DurationMs),
            Status = entity.Status,
            Details = entity.Details,
            TraceId = entity.TraceId,
            DecisionOutcome = entity.DecisionOutcome,
            AffectedSkus = !string.IsNullOrWhiteSpace(entity.AffectedSkusCsv)
                ? entity.AffectedSkusCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                : null,
            AffectedStores = !string.IsNullOrWhiteSpace(entity.AffectedStoresCsv)
                ? entity.AffectedStoresCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                : null
        };
    }
}
