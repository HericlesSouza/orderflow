using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Orders.Create;

public sealed class CreateOrderUseCase(
    IProductRepository productRepository,
    IInventoryItemRepository inventoryItemRepository,
    IOrderRepository orderRepository,
    IUnitOfWork unitOfWork)
{
    public Task<Result<OrderOutput>> ExecuteAsync(
        CreateOrderInput input,
        CancellationToken cancellationToken)
    {
        return unitOfWork.ExecuteInTransactionAsync(ExecuteCreationAsync, cancellationToken);

        async Task<Result<OrderOutput>> ExecuteCreationAsync(CancellationToken transactionCancellationToken)
        {
            var productIds = input.Items.Select(item => item.ProductId).ToArray();
            var products = await productRepository.GetByIdsAsync(productIds, transactionCancellationToken);
            var productsById = products.ToDictionary(product => product.Id);

            if (products.Count != productIds.Length)
            {
                return Result<OrderOutput>.Failure(
                    AppError.NotFound("order.product_not_found", "One or more products were not found."));
            }

            if (products.Any(product => !product.IsActive))
            {
                return Result<OrderOutput>.Failure(
                    AppError.Conflict("order.product_inactive", "One or more products are inactive."));
            }

            var inventoryItems = await inventoryItemRepository.GetByProductIdsForReservationAsync(
                productIds,
                transactionCancellationToken);
            var inventoryItemsByProductId = inventoryItems.ToDictionary(item => item.ProductId);

            if (inventoryItems.Count != productIds.Length)
            {
                return Result<OrderOutput>.Failure(
                    AppError.Conflict(
                        "order.inventory_not_found",
                        "One or more products do not have an inventory item."));
            }

            try
            {
                foreach (var item in input.Items.OrderBy(item => item.ProductId))
                {
                    inventoryItemsByProductId[item.ProductId].Reserve(item.Quantity);
                }

                var orderItems = input.Items
                    .Select(item => OrderItem.Create(
                        item.ProductId,
                        item.Quantity,
                        productsById[item.ProductId].Price))
                    .ToArray();
                var order = Order.Create(orderItems);

                await orderRepository.AddAsync(order, transactionCancellationToken);

                return Result<OrderOutput>.Success(OrderOutput.FromEntity(order));
            }
            catch (InvalidOperationException ex)
            {
                return Result<OrderOutput>.Failure(
                    AppError.Conflict("order.insufficient_inventory", ex.Message));
            }
            catch (ArgumentException ex)
            {
                return Result<OrderOutput>.Failure(
                    AppError.Validation("order.invalid", ex.Message));
            }
        }
    }
}
