namespace SquadCommerce.Mcp.Data.Entities;

/// <summary>
/// EF Core entity representing inventory data.
/// Maps to Inventory table with composite key (StoreId, Sku).
/// </summary>
public sealed class InventoryEntity
{
    public required string StoreId { get; set; }
    public required string StoreName { get; set; }
    public required string Sku { get; set; }
    public required string ProductName { get; set; }
    public required int QuantityOnHand { get; set; }
    public required int ReorderThreshold { get; set; }
    public required DateTimeOffset LastRestocked { get; set; }
}
