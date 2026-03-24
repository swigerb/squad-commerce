namespace SquadCommerce.Contracts.Models;

public record PriceChange
{
    public required string Sku { get; init; }
    public required string StoreId { get; init; }
    public required decimal OldPrice { get; init; }
    public required decimal NewPrice { get; init; }
    public required string Reason { get; init; }
    public required string RequestedBy { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
