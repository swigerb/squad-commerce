namespace SquadCommerce.Mcp.Data.Entities;

/// <summary>
/// EF Core entity representing pricing data.
/// Maps to Pricing table with composite key (StoreId, Sku).
/// </summary>
public sealed class PricingEntity
{
    public required string StoreId { get; set; }
    public required string StoreName { get; set; }
    public required string Sku { get; set; }
    public required string ProductName { get; set; }
    public required decimal CurrentPrice { get; set; }
    public required decimal Cost { get; set; }
    public required decimal MarginPercent { get; set; }
    public required DateTimeOffset LastUpdated { get; set; }
}
