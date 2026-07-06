using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.InventoryItems.Create;

public class CreateInventoryItemUseCase(IInventoryItemRepository inventoryItemRepository, IProductRepository productRepository)
{
    public async Task<Result<InventoryItemOutput>> ExecuteAsync(CreateInventoryItemInput input, CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdAsync(input.ProductId, cancellationToken);

        if (product is null)
        {
            return Result<InventoryItemOutput>.Failure(AppError.NotFound("product.not_found", "Product not found."));
        }

        if (await inventoryItemRepository.ExistsByProductIdAsync(input.ProductId, cancellationToken))
        {
            return Result<InventoryItemOutput>.Failure(
                AppError.Conflict(
                    "inventory_item.already_exists",
                    "An inventory item already exists for this product."));
        }

        try
        {
            var inventoryItem = InventoryItem.Create(input.ProductId, input.Quantity);

            await inventoryItemRepository.AddAsync(inventoryItem, cancellationToken);
            return Result<InventoryItemOutput>.Success(InventoryItemOutput.FromEntity(inventoryItem));
        }
        catch (ArgumentException ex)
        {
            return Result<InventoryItemOutput>.Failure(AppError.Validation("inventory_item.invalid", ex.Message));
        }
        catch (DuplicateInventoryItemException)
        {
            return Result<InventoryItemOutput>.Failure(
                AppError.Conflict(
                    "inventory_item.already_exists",
                    "An inventory item already exists for this product."));
        }
    }
}
