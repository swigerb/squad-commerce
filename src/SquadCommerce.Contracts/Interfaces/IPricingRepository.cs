using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Contracts.Interfaces;

public interface IPricingRepository
{
    Task<PricingUpdateResult> UpdatePricingAsync(PriceChange priceChange, CancellationToken cancellationToken = default);
    Task<decimal?> GetCurrentPriceAsync(string storeId, string sku, CancellationToken cancellationToken = default);
}
