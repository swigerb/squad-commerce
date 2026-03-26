namespace SquadCommerce.Mcp.Data.Entities;

/// <summary>
/// EF Core entity representing supplier compliance data.
/// Maps to Suppliers table with primary key (SupplierId).
/// </summary>
public sealed class SupplierEntity
{
    public required string SupplierId { get; set; }
    public required string Name { get; set; }
    public required string Category { get; set; } // Cocoa, Coffee, Apparel
    public required string Country { get; set; }
    public required string Certification { get; set; } // FairTrade, Organic, RainforestAlliance, None
    public required DateTimeOffset CertificationExpiry { get; set; }
    public required string Status { get; set; } // Compliant, AtRisk, NonCompliant
    public string? WatchlistNotes { get; set; }
}
