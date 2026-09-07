using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Orders;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Payments.Reject;

public sealed class RejectPaymentUseCase(
    IOrderRepository orderRepository,
    IInventoryItemRepository inventoryItemRepository,
    IUnitOfWork unitOfWork)
{
    public Task<Result<OrderOutput>> ExecuteAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransactionAsync(
            transactionCancellationToken => RejectAsync(orderId, transactionCancellationToken),
            cancellationToken);
    }

    private async Task<Result<OrderOutput>> RejectAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdForUpdateAsync(orderId, cancellationToken);

        if (order is null)
        {
            return Result<OrderOutput>.Failure(
                AppError.NotFound("order.not_found", "Order not found."));
        }

        try
        {
            order.EnsurePaymentCanBeRejected();
        }
        catch (InvalidOperationException ex)
        {
            return Result<OrderOutput>.Failure(
                AppError.Conflict("payment.order_not_pending", ex.Message));
        }

        var productIds = order.OrderItems.Select(item => item.ProductId).ToArray();
        var inventoryItems = await inventoryItemRepository.GetByProductIdsForUpdateAsync(productIds, cancellationToken);
        var inventoryItemsByProductId = inventoryItems.ToDictionary(item => item.ProductId);

        if (order.OrderItems.Any(item => !inventoryItemsByProductId.TryGetValue(item.ProductId, out var inventoryItem) ||
                                         inventoryItem.ReservedQuantity < item.Quantity))
        {
            return Result<OrderOutput>.Failure(
                AppError.Conflict(
                    "order.inventory_inconsistent",
                    "One or more inventory items are missing or do not contain enough reserved quantity."));
        }

        foreach (var item in order.OrderItems.OrderBy(item => item.ProductId))
        {
            inventoryItemsByProductId[item.ProductId].Release(item.Quantity);
        }

        order.RejectPayment();

        return Result<OrderOutput>.Success(OrderOutput.FromEntity(order));
    }
}
