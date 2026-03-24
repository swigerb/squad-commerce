namespace SquadCommerce.Contracts.Models;

public record InventorySnapshot
{
    public required string StoreId { get; init; }
    public required string Sku { get; init; }
    public required int UnitsOnHand { get; init; }
    public required int ReorderPoint { get; init; }
    public required int UnitsOnOrder { get; init; }
    public required DateTimeOffset LastUpdated { get; init; }
}
