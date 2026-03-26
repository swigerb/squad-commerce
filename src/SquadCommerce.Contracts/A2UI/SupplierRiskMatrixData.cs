namespace SquadCommerce.Contracts.A2UI;

public sealed record SupplierRiskMatrixData
{
    public required string ProductCategory { get; init; }
    public required string CertificationRequired { get; init; }
    public required IReadOnlyList<SupplierRiskEntry> Suppliers { get; init; }
    public required int TotalAtRisk { get; init; }
    public required int TotalCompliant { get; init; }
    public required int TotalNonCompliant { get; init; }
    public required DateTimeOffset Deadline { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record SupplierRiskEntry
{
    public required string SupplierId { get; init; }
    public required string SupplierName { get; init; }
    public required string Country { get; init; }
    public required string Certification { get; init; }
    public required DateTimeOffset? CertificationExpiry { get; init; }
    public required string RiskLevel { get; init; } // Compliant, AtRisk, NonCompliant
    public required string? WatchlistNotes { get; init; }
    public IReadOnlyList<string>? AlternativeSuppliers { get; init; }
}
