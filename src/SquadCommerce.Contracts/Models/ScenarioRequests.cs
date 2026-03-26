namespace SquadCommerce.Contracts.Models;

public sealed record SupplyChainShockRequest
{
    public required string Sku { get; init; }
    public required int DelayDays { get; init; }
    public required string Reason { get; init; }
    public required string[] AffectedRegions { get; init; }
    public required string SessionId { get; init; }
}

public sealed record ViralSpikeRequest
{
    public required string Sku { get; init; }
    public required decimal DemandMultiplier { get; init; }
    public required string Region { get; init; }
    public required string Source { get; init; } // "TikTok", "Instagram", etc.
    public required string SessionId { get; init; }
}

public sealed record ESGAuditRequest
{
    public required string ProductCategory { get; init; } // "Cocoa", "Coffee"
    public required string CertificationRequired { get; init; } // "FairTrade", "Organic"
    public required DateTimeOffset Deadline { get; init; }
    public required string SessionId { get; init; }
}

public sealed record StoreReadinessRequest
{
    public required string StoreId { get; init; }
    public required string Section { get; init; } // "Electronics", "Grocery"
    public required DateTimeOffset OpeningDate { get; init; }
    public required string SessionId { get; init; }
}
