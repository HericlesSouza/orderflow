using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Repositories;

public interface IInventoryItemRepository
{
    Task AddAsync(InventoryItem inventoryItem, CancellationToken cancellationToken);
    Task<bool> ExistsByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);
    Task<InventoryItem?> GetByProductIdForUpdateAsync(
        Guid productId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryItem>> GetByProductIdsForUpdateAsync(
        IReadOnlyCollection<Guid> productIds,
        CancellationToken cancellationToken);
}
