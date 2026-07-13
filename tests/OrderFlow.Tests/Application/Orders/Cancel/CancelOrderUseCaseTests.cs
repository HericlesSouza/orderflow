using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Orders;
using OrderFlow.Application.Orders.Cancel;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Tests.Application.Orders.Cancel;

public sealed class CancelOrderUseCaseTests
{
    [Fact]
    public async Task Should_Cancel_Order_And_Release_All_Reserved_Inventory_When_Order_Is_PendingPayment()
    {
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var order = Order.Create([
            OrderItem.Create(firstProductId, 2, 100.00m),
            OrderItem.Create(secondProductId, 3, 50.00m)
        ]);
        var firstInventory = CreateReservedInventory(firstProductId, 5, 2);
        var secondInventory = CreateReservedInventory(secondProductId, 10, 3);
        var unitOfWork = new ImmediateUnitOfWork();
        var useCase = CreateUseCase(order, [firstInventory, secondInventory], unitOfWork);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Cancelled", result.Value!.Status);
        Assert.NotNull(result.Value.CancelledAt);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(0, firstInventory.ReservedQuantity);
        Assert.Equal(5, firstInventory.AvailableQuantity);
        Assert.Equal(0, secondInventory.ReservedQuantity);
        Assert.Equal(10, secondInventory.AvailableQuantity);
        Assert.Equal(1, unitOfWork.ExecutionCount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        var useCase = CreateUseCase(order: null, inventoryItems: []);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        AssertFailure(result, "order.not_found", AppErrorType.NotFound);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Release_Inventory_When_Order_Is_Already_Cancelled()
    {
        var (order, inventory) = CreateOrderWithReservedInventory();
        order.Cancel();
        var useCase = CreateUseCase(order, [inventory]);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.cannot_cancel", AppErrorType.Conflict);
        Assert.Equal(2, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Release_Inventory_When_Order_Is_Confirmed()
    {
        var (order, inventory) = CreateOrderWithReservedInventory();
        order.ConfirmPayment();
        var useCase = CreateUseCase(order, [inventory]);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.cannot_cancel", AppErrorType.Conflict);
        Assert.Equal(2, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Release_Inventory_When_Order_Is_PaymentRejected()
    {
        var (order, inventory) = CreateOrderWithReservedInventory();
        order.RejectPayment();
        var useCase = CreateUseCase(order, [inventory]);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.cannot_cancel", AppErrorType.Conflict);
        Assert.Equal(2, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Cancel_Order_When_Inventory_Does_Not_Exist()
    {
        var (order, _) = CreateOrderWithReservedInventory();
        var useCase = CreateUseCase(order, []);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.inventory_inconsistent", AppErrorType.Conflict);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Null(order.CancelledAt);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Cancel_Order_When_Reserved_Inventory_Is_Insufficient()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create([OrderItem.Create(productId, 2, 100.00m)]);
        var inventory = CreateReservedInventory(productId, 5, 1);
        var useCase = CreateUseCase(order, [inventory]);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.inventory_inconsistent", AppErrorType.Conflict);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(1, inventory.ReservedQuantity);
    }

    private static CancelOrderUseCase CreateUseCase(
        Order? order,
        IReadOnlyList<InventoryItem> inventoryItems,
        ImmediateUnitOfWork? unitOfWork = null)
    {
        var orderRepository = new Mock<IOrderRepository>();
        orderRepository
            .Setup(repository => repository.GetByIdForUpdateAsync(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var inventoryRepository = new Mock<IInventoryItemRepository>();
        inventoryRepository
            .Setup(repository => repository.GetByProductIdsForUpdateAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItems);

        return new CancelOrderUseCase(
            orderRepository.Object,
            inventoryRepository.Object,
            unitOfWork ?? new ImmediateUnitOfWork());
    }

    private static (Order Order, InventoryItem Inventory) CreateOrderWithReservedInventory()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create([OrderItem.Create(productId, 2, 100.00m)]);
        var inventory = CreateReservedInventory(productId, 5, 2);

        return (order, inventory);
    }

    private static InventoryItem CreateReservedInventory(Guid productId, int quantity, int reservedQuantity)
    {
        var inventory = InventoryItem.Create(productId, quantity);
        inventory.Reserve(reservedQuantity);
        return inventory;
    }

    private static void AssertFailure(Result<OrderOutput> result, string code, AppErrorType type)
    {
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(type, result.Error.Type);
    }

    private sealed class ImmediateUnitOfWork : IUnitOfWork
    {
        public int ExecutionCount { get; private set; }

        public async Task<Result<T>> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<Result<T>>> operation,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return await operation(cancellationToken);
        }
    }
}
