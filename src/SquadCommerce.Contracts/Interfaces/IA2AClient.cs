using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Contracts.Interfaces;

public interface IA2AClient
{
    Task<IReadOnlyList<CompetitorPricing>> GetCompetitorPricingAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompetitorPricing>> GetBulkCompetitorPricingAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken = default);
    Task<bool> ValidateExternalDataAsync(CompetitorPricing competitorData, CancellationToken cancellationToken = default);
}
