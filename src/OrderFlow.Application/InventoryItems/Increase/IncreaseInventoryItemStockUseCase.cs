using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.InventoryItems;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.InventoryItems.Increase;

public class IncreaseInventoryItemStockUseCase(IInventoryItemRepository inventoryItemRepository)
{
    public async Task<Result<InventoryItemOutput>> ExecuteAsync(
        IncreaseInventoryItemStockInput input,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await inventoryItemRepository.GetByProductIdAsync(input.ProductId, cancellationToken);

        if (inventoryItem is null)
        {
            return Result<InventoryItemOutput>.Failure(
                AppError.NotFound(
                    "inventory_item.not_found",
                    "No inventory item found for the specified product."));
        }

        try
        {
            inventoryItem.IncreaseStock(input.Quantity);

            await inventoryItemRepository.UpdateAsync(inventoryItem, cancellationToken);

            return Result<InventoryItemOutput>.Success(InventoryItemOutput.FromEntity(inventoryItem));
        }
        catch (ArgumentException ex)
        {
            return Result<InventoryItemOutput>.Failure(AppError.Validation("inventory_item.invalid", ex.Message));
        }
    }
}
