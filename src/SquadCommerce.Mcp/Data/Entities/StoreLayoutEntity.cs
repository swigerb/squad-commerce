namespace SquadCommerce.Mcp.Data.Entities;

/// <summary>
/// EF Core entity representing store layout and foot traffic data.
/// Maps to StoreLayouts table with auto-incremented primary key (Id).
/// </summary>
public sealed class StoreLayoutEntity
{
    public int Id { get; set; }
    public required string StoreId { get; set; }
    public required string StoreName { get; set; }
    public required string Section { get; set; } // Electronics, Grocery, Apparel, Home
    public required int SquareFootage { get; set; }
    public required int ShelfCount { get; set; }
    public required double AvgHourlyTraffic { get; set; }
    public required string OptimalPlacement { get; set; } // Front, Middle, Back, EndCap
}
