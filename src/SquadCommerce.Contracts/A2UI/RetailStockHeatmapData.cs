namespace SquadCommerce.Contracts.A2UI;

public record RetailStockHeatmapData
{
    public required IReadOnlyList<StoreStockLevel> Stores { get; init; }
    public required string Sku { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public record StoreStockLevel
{
    public required string StoreId { get; init; }
    public required string StoreName { get; init; }
    public required int UnitsOnHand { get; init; }
    public required int ReorderPoint { get; init; }
    public required string StockStatus { get; init; }
}
