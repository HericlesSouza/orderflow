using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Orders.Cancel;

public sealed class CancelOrderUseCase(
    IOrderRepository orderRepository,
    IInventoryItemRepository inventoryItemRepository,
    IUnitOfWork unitOfWork)
{
    public Task<Result<OrderOutput>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransactionAsync(
            transactionCancellationToken => CancelOrderAsync(orderId, transactionCancellationToken),
            cancellationToken);
    }

    private async Task<Result<OrderOutput>> CancelOrderAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);

        if (order is null)
        {
            return Result<OrderOutput>.Failure(
                AppError.NotFound("order.not_found", "Order not found."));
        }

        var productIds = order.OrderItems.Select(item => item.ProductId).ToArray();
        var inventoryItems = await inventoryItemRepository.GetByProductIdsForUpdateAsync(productIds, cancellationToken);
        var inventoryItemsByProductId = inventoryItems.ToDictionary(item => item.ProductId);

        var inventoryValidation = ValidateReservedInventory(order.OrderItems, inventoryItemsByProductId);

        if (inventoryValidation.IsFailure)
        {
            return Result<OrderOutput>.Failure(inventoryValidation.Error!);
        }

        try
        {
            order.Cancel();
        }
        catch (InvalidOperationException ex)
        {
            return Result<OrderOutput>.Failure(
                AppError.Conflict("order.cannot_cancel", ex.Message));
        }

        foreach (var item in order.OrderItems.OrderBy(item => item.ProductId))
        {
            inventoryItemsByProductId[item.ProductId].Release(item.Quantity);
        }

        return Result<OrderOutput>.Success(OrderOutput.FromEntity(order));
    }

    private static Result ValidateReservedInventory(
        IReadOnlyCollection<OrderItem> orderItems,
        IReadOnlyDictionary<Guid, InventoryItem> inventoryItemsByProductId)
    {
        if (orderItems.Any(item => !inventoryItemsByProductId.ContainsKey(item.ProductId)) ||
            orderItems.Any(item => inventoryItemsByProductId[item.ProductId].ReservedQuantity < item.Quantity))
        {
            return Result.Failure(
                AppError.Conflict(
                    "order.inventory_inconsistent",
                    "One or more inventory items are missing or do not contain enough reserved quantity."));
        }

        return Result.Success();
    }
}
