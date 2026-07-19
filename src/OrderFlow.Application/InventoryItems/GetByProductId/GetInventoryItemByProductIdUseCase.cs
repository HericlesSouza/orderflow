using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.InventoryItems.GetByProductId;

public class GetInventoryItemByProductIdUseCase(IInventoryItemRepository inventoryItemRepository)
{
    public async Task<Result<InventoryItemOutput?>> ExecuteAsync(Guid productId, CancellationToken cancellationToken)
    {
        var inventoryItem = await inventoryItemRepository.GetByProductIdAsync(productId, cancellationToken);

        if (inventoryItem is null)
        {
            return Result<InventoryItemOutput?>.Failure(
                AppError.NotFound(
                    "inventory_item.not_found",
                    "No inventory item found for the specified product."));
        }

        return Result<InventoryItemOutput?>.Success(InventoryItemOutput.FromEntity(inventoryItem));
    }
}
