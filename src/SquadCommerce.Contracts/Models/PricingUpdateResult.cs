namespace SquadCommerce.Contracts.Models;

public record PricingUpdateResult
{
    public required string Sku { get; init; }
    public required IReadOnlyList<string> StoresUpdated { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
