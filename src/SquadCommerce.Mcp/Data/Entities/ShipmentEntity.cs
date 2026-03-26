namespace SquadCommerce.Mcp.Data.Entities;

/// <summary>
/// EF Core entity representing shipment tracking data.
/// Maps to Shipments table with primary key (ShipmentId).
/// </summary>
public sealed class ShipmentEntity
{
    public required string ShipmentId { get; set; }
    public required string Sku { get; set; }
    public required string ProductName { get; set; }
    public required string OriginStoreId { get; set; }
    public required string DestStoreId { get; set; }
    public required string Status { get; set; } // InTransit, Delayed, Delivered
    public required DateTimeOffset EstimatedArrival { get; set; }
    public required int DelayDays { get; set; }
    public string? DelayReason { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}
