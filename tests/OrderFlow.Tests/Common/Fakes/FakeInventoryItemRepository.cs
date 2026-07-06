using OrderFlow.Application.InventoryItems;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests.Common.Fakes;

public sealed class FakeInventoryItemRepository(List<InventoryItem>? inventoryItems = null) : IInventoryItemRepository
{
    private readonly List<InventoryItem> _inventoryItems = inventoryItems ?? [];

    public IReadOnlyList<InventoryItem> InventoryItems => _inventoryItems;
    public int AddAsyncCallCount { get; private set; }
    public bool ThrowDuplicateOnAdd { get; set; }

    public Task AddAsync(InventoryItem inventoryItem, CancellationToken cancellationToken)
    {
        AddAsyncCallCount++;

        if (ThrowDuplicateOnAdd)
            throw new DuplicateInventoryItemException(inventoryItem.ProductId);

        _inventoryItems.Add(inventoryItem);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        return Task.FromResult(_inventoryItems.Any(inventoryItem => inventoryItem.ProductId == productId));
    }
}
