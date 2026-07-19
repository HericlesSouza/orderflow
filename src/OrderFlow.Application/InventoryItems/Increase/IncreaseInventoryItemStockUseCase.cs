using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Application.InventoryItems.Increase;

public class IncreaseInventoryItemStockUseCase(
    IInventoryItemRepository inventoryItemRepository,
    IUnitOfWork unitOfWork)
{
    public Task<Result<InventoryItemOutput>> ExecuteAsync(
        IncreaseInventoryItemStockInput input,
        CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransactionAsync(
            transactionCancellationToken => IncreaseStockAsync(input, transactionCancellationToken),
            cancellationToken);
    }

    private async Task<Result<InventoryItemOutput>> IncreaseStockAsync(
        IncreaseInventoryItemStockInput input,
        CancellationToken cancellationToken)
    {
        var inventoryItem = await inventoryItemRepository.GetByProductIdForUpdateAsync(
            input.ProductId,
            cancellationToken);

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

            return Result<InventoryItemOutput>.Success(InventoryItemOutput.FromEntity(inventoryItem));
        }
        catch (ArgumentException ex)
        {
            return Result<InventoryItemOutput>.Failure(AppError.Validation("inventory_item.invalid", ex.Message));
        }
    }
}
