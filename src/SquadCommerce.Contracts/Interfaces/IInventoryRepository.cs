using SquadCommerce.Contracts.Models;

namespace SquadCommerce.Contracts.Interfaces;

public interface IInventoryRepository
{
    Task<IReadOnlyList<InventorySnapshot>> GetInventoryLevelsAsync(string sku, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventorySnapshot>> GetBulkInventoryLevelsAsync(IReadOnlyList<string> skus, CancellationToken cancellationToken = default);
    Task<InventorySnapshot?> GetInventoryForStoreAsync(string storeId, string sku, CancellationToken cancellationToken = default);
}
