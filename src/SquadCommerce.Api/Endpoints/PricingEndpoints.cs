using Microsoft.AspNetCore.Http.HttpResults;
using SquadCommerce.Contracts.A2UI;
using SquadCommerce.Mcp.Data;
using SquadCommerce.Observability;

namespace SquadCommerce.Api.Endpoints;

public static class PricingEndpoints
{
    public static IEndpointRouteBuilder MapPricingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pricing")
            .WithTags("Pricing");

        group.MapPost("/approve", ApproveProposal)
            .WithName("ApproveProposal")
            .WithSummary("Approve pricing proposal and execute store updates");

        group.MapPost("/reject", RejectProposal)
            .WithName("RejectProposal")
            .WithSummary("Reject pricing proposal with no action taken");

        group.MapPost("/modify", ModifyProposal)
            .WithName("ModifyProposal")
            .WithSummary("Modify proposed prices and re-trigger calculation");

        group.MapPost("/approve/bulk", ApproveBulkProposal)
            .WithName("ApproveBulkProposal")
            .WithSummary("Approve multiple pricing proposals in bulk");

        group.MapPost("/reject/bulk", RejectBulkProposal)
            .WithName("RejectBulkProposal")
            .WithSummary("Reject multiple pricing proposals in bulk");
        
        return app;
    }

    /// <summary>
    /// Approves a pricing proposal and triggers PricingAgent to execute UpdateStorePricing MCP tool.
    /// </summary>
    private static async Task<Ok<PricingActionResponse>> ApproveProposal(
        PricingApprovalRequest request,
        AuditRepository auditRepository,
        SquadCommerceMetrics metrics,
        ILogger<PricingApprovalRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Pricing proposal approved: ProposalId={ProposalId}, ApprovedBy={ApprovedBy}", 
            request.ProposalId, request.ApprovedBy);

        // Record pricing decision metric
        metrics.RecordPricingDecision("approved", request.ProposalId);

        // Record audit entry for human decision
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            AgentName = "PricingManager",
            Action = "Reviewed pricing recommendation",
            Protocol = "Internal",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            Status = "Success",
            Details = $"Approved by {request.ApprovedBy}",
            DecisionOutcome = "Approved",
            AffectedStores = request.StoreIds
        };
        await auditRepository.RecordAuditEntryAsync(request.ProposalId, auditEntry, cancellationToken);

        // Mock implementation - will invoke PricingAgent.UpdateStorePricing via MCP
        await Task.Delay(100, cancellationToken);

        return TypedResults.Ok(new PricingActionResponse
        {
            ProposalId = request.ProposalId,
            Action = "Approved",
            Success = true,
            Message = $"Pricing updates applied to {request.StoreIds.Count} store(s). PricingAgent executed UpdateStorePricing MCP tool.",
            UpdatedStores = request.StoreIds,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Rejects a pricing proposal. Logs the rejection with no further action.
    /// </summary>
    private static async Task<Ok<PricingActionResponse>> RejectProposal(
        PricingRejectionRequest request,
        AuditRepository auditRepository,
        SquadCommerceMetrics metrics,
        ILogger<PricingRejectionRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Pricing proposal rejected: ProposalId={ProposalId}, RejectedBy={RejectedBy}, Reason={Reason}", 
            request.ProposalId, request.RejectedBy, request.Reason);

        // Record pricing decision metric
        metrics.RecordPricingDecision("rejected", request.ProposalId);

        // Record audit entry for human decision
        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            AgentName = "PricingManager",
            Action = "Reviewed pricing recommendation",
            Protocol = "Internal",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            Status = "Success",
            Details = $"Rejected by {request.RejectedBy}: {request.Reason}",
            DecisionOutcome = "Rejected"
        };
        await auditRepository.RecordAuditEntryAsync(request.ProposalId, auditEntry, cancellationToken);

        return TypedResults.Ok(new PricingActionResponse
        {
            ProposalId = request.ProposalId,
            Action = "Rejected",
            Success = true,
            Message = $"Proposal rejected. Reason: {request.Reason}",
            UpdatedStores = Array.Empty<string>(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Modifies a pricing proposal with new prices and re-triggers PricingAgent calculation.
    /// </summary>
    private static async Task<Accepted<PricingActionResponse>> ModifyProposal(
        PricingModificationRequest request,
        AuditRepository auditRepository,
        SquadCommerceMetrics metrics,
        ILogger<PricingModificationRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Pricing proposal modified: ProposalId={ProposalId}, ModifiedBy={ModifiedBy}, ModifiedPrices={Count}", 
            request.ProposalId, request.ModifiedBy, request.ModifiedPrices.Count);

        // Record pricing decision metric
        metrics.RecordPricingDecision("modified", request.ProposalId);

        // Record audit entry for human decision
        var modifiedSkus = request.ModifiedPrices.Select(p => p.Sku).Distinct().ToList();
        var modifiedStores = request.ModifiedPrices.Select(p => p.StoreId).Distinct().ToList();
        var priceDetails = string.Join(", ", request.ModifiedPrices.Select(p => $"{p.Sku} @ {p.StoreId}: ${p.NewPrice:F2}"));

        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            AgentName = "PricingManager",
            Action = "Modified pricing recommendation",
            Protocol = "Internal",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            Status = "Success",
            Details = $"Modified by {request.ModifiedBy}: {priceDetails}",
            DecisionOutcome = "Modified",
            AffectedSkus = modifiedSkus,
            AffectedStores = modifiedStores
        };
        await auditRepository.RecordAuditEntryAsync(request.ProposalId, auditEntry, cancellationToken);

        // Mock implementation - will re-invoke PricingAgent with new prices
        await Task.Delay(100, cancellationToken);

        return TypedResults.Accepted($"/api/pricing/proposals/{request.ProposalId}", new PricingActionResponse
        {
            ProposalId = request.ProposalId,
            Action = "Modified",
            Success = true,
            Message = "Modified prices received. Re-triggering PricingAgent calculation with new values.",
            UpdatedStores = modifiedStores,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Approves multiple pricing proposals in bulk.
    /// </summary>
    private static async Task<Ok<PricingActionResponse>> ApproveBulkProposal(
        BulkPricingApprovalRequest request,
        AuditRepository auditRepository,
        SquadCommerceMetrics metrics,
        ILogger<BulkPricingApprovalRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Bulk pricing proposal approved: ProposalId={ProposalId}, ApprovedBy={ApprovedBy}, Items={ItemCount}", 
            request.ProposalId, request.ApprovedBy, request.Items.Count);

        metrics.RecordPricingDecision("bulk-approved", request.ProposalId);

        var allAffectedSkus = request.Items.Select(i => i.Sku).Distinct().ToList();
        var allAffectedStores = request.Items.SelectMany(i => i.StoreIds).Distinct().ToList();

        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            AgentName = "PricingManager",
            Action = "Reviewed bulk pricing recommendation",
            Protocol = "Internal",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            Status = "Success",
            Details = $"Bulk approved by {request.ApprovedBy} for {request.Items.Count} SKUs",
            DecisionOutcome = "Approved",
            AffectedSkus = allAffectedSkus,
            AffectedStores = allAffectedStores
        };
        await auditRepository.RecordAuditEntryAsync(request.ProposalId, auditEntry, cancellationToken);

        await Task.Delay(100, cancellationToken);

        return TypedResults.Ok(new PricingActionResponse
        {
            ProposalId = request.ProposalId,
            Action = "BulkApproved",
            Success = true,
            Message = $"Bulk pricing updates applied to {request.Items.Count} SKUs across {allAffectedStores.Count} store(s). PricingAgent executed UpdateStorePricing MCP tool.",
            UpdatedStores = allAffectedStores,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    /// <summary>
    /// Rejects multiple pricing proposals in bulk.
    /// </summary>
    private static async Task<Ok<PricingActionResponse>> RejectBulkProposal(
        BulkPricingRejectionRequest request,
        AuditRepository auditRepository,
        SquadCommerceMetrics metrics,
        ILogger<BulkPricingRejectionRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Bulk pricing proposal rejected: ProposalId={ProposalId}, RejectedBy={RejectedBy}, Reason={Reason}", 
            request.ProposalId, request.RejectedBy, request.Reason);

        metrics.RecordPricingDecision("bulk-rejected", request.ProposalId);

        var auditEntry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            AgentName = "PricingManager",
            Action = "Reviewed bulk pricing recommendation",
            Protocol = "Internal",
            Timestamp = DateTimeOffset.UtcNow,
            Duration = TimeSpan.Zero,
            Status = "Success",
            Details = $"Bulk rejected by {request.RejectedBy}: {request.Reason}",
            DecisionOutcome = "Rejected"
        };
        await auditRepository.RecordAuditEntryAsync(request.ProposalId, auditEntry, cancellationToken);

        return TypedResults.Ok(new PricingActionResponse
        {
            ProposalId = request.ProposalId,
            Action = "BulkRejected",
            Success = true,
            Message = $"Bulk proposal rejected. Reason: {request.Reason}",
            UpdatedStores = Array.Empty<string>(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}

public sealed record BulkPricingApprovalRequest
{
    public required string ProposalId { get; init; }
    public required string ApprovedBy { get; init; }
    public required IReadOnlyList<BulkApprovalItem> Items { get; init; }
}

public sealed record BulkApprovalItem
{
    public required string Sku { get; init; }
    public required IReadOnlyList<string> StoreIds { get; init; }
}

public sealed record BulkPricingRejectionRequest
{
    public required string ProposalId { get; init; }
    public required string RejectedBy { get; init; }
    public required string Reason { get; init; }
}

public sealed record PricingApprovalRequest
{
    public required string ProposalId { get; init; }
    public required string ApprovedBy { get; init; }
    public required IReadOnlyList<string> StoreIds { get; init; }
}

public sealed record PricingRejectionRequest
{
    public required string ProposalId { get; init; }
    public required string RejectedBy { get; init; }
    public required string Reason { get; init; }
}

public sealed record PricingModificationRequest
{
    public required string ProposalId { get; init; }
    public required string ModifiedBy { get; init; }
    public required IReadOnlyList<ModifiedPrice> ModifiedPrices { get; init; }
}

public sealed record ModifiedPrice
{
    public required string Sku { get; init; }
    public required string StoreId { get; init; }
    public required decimal NewPrice { get; init; }
}

public sealed record PricingActionResponse
{
    public required string ProposalId { get; init; }
    public required string Action { get; init; }
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required IReadOnlyList<string> UpdatedStores { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
