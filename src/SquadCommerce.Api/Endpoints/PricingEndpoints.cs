using Microsoft.AspNetCore.Http.HttpResults;
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
        
        return app;
    }

    /// <summary>
    /// Approves a pricing proposal and triggers PricingAgent to execute UpdateStorePricing MCP tool.
    /// </summary>
    private static async Task<Ok<PricingActionResponse>> ApproveProposal(
        PricingApprovalRequest request,
        SquadCommerceMetrics metrics,
        ILogger<PricingApprovalRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Pricing proposal approved: ProposalId={ProposalId}, ApprovedBy={ApprovedBy}", 
            request.ProposalId, request.ApprovedBy);

        // Record pricing decision metric
        metrics.RecordPricingDecision("approved", request.ProposalId);

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
    private static Ok<PricingActionResponse> RejectProposal(
        PricingRejectionRequest request,
        SquadCommerceMetrics metrics,
        ILogger<PricingRejectionRequest> logger)
    {
        logger.LogInformation("Pricing proposal rejected: ProposalId={ProposalId}, RejectedBy={RejectedBy}, Reason={Reason}", 
            request.ProposalId, request.RejectedBy, request.Reason);

        // Record pricing decision metric
        metrics.RecordPricingDecision("rejected", request.ProposalId);

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
        SquadCommerceMetrics metrics,
        ILogger<PricingModificationRequest> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Pricing proposal modified: ProposalId={ProposalId}, ModifiedBy={ModifiedBy}, ModifiedPrices={Count}", 
            request.ProposalId, request.ModifiedBy, request.ModifiedPrices.Count);

        // Record pricing decision metric
        metrics.RecordPricingDecision("modified", request.ProposalId);

        // Mock implementation - will re-invoke PricingAgent with new prices
        await Task.Delay(100, cancellationToken);

        return TypedResults.Accepted($"/api/pricing/proposals/{request.ProposalId}", new PricingActionResponse
        {
            ProposalId = request.ProposalId,
            Action = "Modified",
            Success = true,
            Message = "Modified prices received. Re-triggering PricingAgent calculation with new values.",
            UpdatedStores = request.ModifiedPrices.Select(p => p.StoreId).Distinct().ToArray(),
            Timestamp = DateTimeOffset.UtcNow
        });
    }
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
