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
        return unitOfWork.ExecuteInTransactionAsync(
            transactionCancellationToken =>
                CreateOrderAsync(input, transactionCancellationToken),
            cancellationToken);
    }

    private async Task<Result<OrderOutput>> CreateOrderAsync(CreateOrderInput input, CancellationToken cancellationToken)
    {
        var productIds = input.Items.Select(item => item.ProductId).ToArray();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
        var productsById = products.ToDictionary(product => product.Id);

        var productValidation = ValidateProducts(productIds, productsById);

        if (productValidation.IsFailure)
            return Result<OrderOutput>.Failure(productValidation.Error!);

        var inventoryItems = await inventoryItemRepository.GetByProductIdsForUpdateAsync(
            productIds,
            cancellationToken);
        var inventoryItemsByProductId = inventoryItems.ToDictionary(item => item.ProductId);

        var inventoryValidation = ValidateInventory(input.Items, inventoryItemsByProductId);

        if (inventoryValidation.IsFailure)
            return Result<OrderOutput>.Failure(inventoryValidation.Error!);

        var reservation = ReserveInventory(input.Items, inventoryItemsByProductId);

        if (reservation.IsFailure)
            return Result<OrderOutput>.Failure(reservation.Error!);

        Order order;

        try
        {
            var orderItems = CreateOrderItems(input.Items, productsById);
            order = Order.Create(orderItems);
        }
        catch (ArgumentException ex)
        {
            return Result<OrderOutput>.Failure(
                AppError.Validation("order.invalid", ex.Message));
        }

        await orderRepository.AddAsync(order, cancellationToken);

        return Result<OrderOutput>.Success(OrderOutput.FromEntity(order));
    }

    private static Result ValidateProducts(
        IReadOnlyCollection<Guid> productIds,
        IReadOnlyDictionary<Guid, Product> productsById)
    {
        if (productIds.Any(productId => !productsById.ContainsKey(productId)))
        {
            return Result.Failure(
                AppError.NotFound("order.product_not_found", "One or more products were not found."));
        }

        if (productsById.Values.Any(product => !product.IsActive))
        {
            return Result.Failure(
                AppError.Conflict("order.product_inactive", "One or more products are inactive."));
        }

        return Result.Success();
    }

    private static Result ValidateInventory(
        IReadOnlyCollection<CreateOrderItemInput> orderItems,
        IReadOnlyDictionary<Guid, InventoryItem> inventoryItemsByProductId)
    {
        if (orderItems.Any(item => !inventoryItemsByProductId.ContainsKey(item.ProductId)))
        {
            return Result.Failure(
                AppError.Conflict(
                    "order.inventory_not_found",
                    "One or more products do not have an inventory item."));
        }

        if (orderItems.Any(item =>
                inventoryItemsByProductId[item.ProductId].AvailableQuantity < item.Quantity))
        {
            return Result.Failure(
                AppError.Conflict(
                    "order.insufficient_inventory",
                    "Not enough available quantity to reserve."));
        }

        return Result.Success();
    }

    private static Result ReserveInventory(
        IReadOnlyCollection<CreateOrderItemInput> orderItems,
        IReadOnlyDictionary<Guid, InventoryItem> inventoryItemsByProductId)
    {
        try
        {
            foreach (var item in orderItems.OrderBy(item => item.ProductId))
            {
                inventoryItemsByProductId[item.ProductId].Reserve(item.Quantity);
            }

            return Result.Success();
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(
                AppError.Conflict("order.insufficient_inventory", ex.Message));
        }
        catch (ArgumentException ex)
        {
            return Result.Failure(
                AppError.Validation("order.invalid", ex.Message));
        }
    }

    private static OrderItem[] CreateOrderItems(
        IReadOnlyCollection<CreateOrderItemInput> orderItems,
        IReadOnlyDictionary<Guid, Product> productsById)
    {
        return orderItems
            .Select(item => OrderItem.Create(
                item.ProductId,
                item.Quantity,
                productsById[item.ProductId].Price))
            .ToArray();
    }
}
