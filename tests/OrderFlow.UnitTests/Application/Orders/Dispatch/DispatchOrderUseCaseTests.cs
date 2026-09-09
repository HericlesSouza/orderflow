using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Orders.Dispatch;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.UnitTests.Common.Builders;
using OrderFlow.UnitTests.Common.Fakes;

namespace OrderFlow.UnitTests.Application.Orders.Dispatch;

[Trait("Layer", "Application")]
[Trait("Feature", "Orders")]
public sealed class DispatchOrderUseCaseTests
{
    [Fact]
    public async Task Should_Dispatch_Confirmed_Order_And_Deduct_All_Reserved_Inventory()
    {
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(
            new OrderItemBuilder().ForProduct(firstProductId).WithQuantity(2).WithUnitPrice(100).Build(),
            new OrderItemBuilder().ForProduct(secondProductId).WithQuantity(3).WithUnitPrice(50).Build()).Build();
        order.ConfirmPayment();
        var firstInventory = CreateReservedInventory(firstProductId, 5, 2);
        var secondInventory = CreateReservedInventory(secondProductId, 10, 3);
        var unitOfWork = new ImmediateUnitOfWork();

        var result = await CreateUseCase(order, [firstInventory, secondInventory], unitOfWork)
            .ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Dispatched", result.Value!.Status);
        Assert.NotNull(result.Value.DispatchedAt);
        Assert.Equal(OrderStatus.Dispatched, order.Status);
        Assert.Equal(3, firstInventory.Quantity);
        Assert.Equal(0, firstInventory.ReservedQuantity);
        Assert.Equal(7, secondInventory.Quantity);
        Assert.Equal(0, secondInventory.ReservedQuantity);
        Assert.Equal(1, unitOfWork.ExecutionCount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        var result = await CreateUseCase(null, []).ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AppErrorType.NotFound, result.Error!.Type);
        Assert.Equal("order.not_found", result.Error.Code);
    }

    [Theory]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    [InlineData(OrderStatus.Dispatched)]
    public async Task Should_Return_Conflict_Without_Changing_Inventory_When_Order_Cannot_Be_Dispatched(OrderStatus status)
    {
        var productId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(new OrderItemBuilder().ForProduct(productId).WithQuantity(2).Build()).Build();
        var inventory = CreateReservedInventory(productId, 5, 2);
        switch (status)
        {
            case OrderStatus.Cancelled: order.Cancel(); inventory.Release(2); break;
            case OrderStatus.PaymentRejected: order.RejectPayment(); inventory.Release(2); break;
            case OrderStatus.Dispatched: order.ConfirmPayment(); order.Dispatch(); inventory.Dispatch(2); break;
        }

        var result = await CreateUseCase(order, [inventory]).ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AppErrorType.Conflict, result.Error!.Type);
        Assert.Equal("order.cannot_dispatch", result.Error.Code);
        Assert.Equal(status, order.Status);
        Assert.Equal(status == OrderStatus.PendingPayment ? 2 : 0, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task Should_Not_Dispatch_Order_When_Reserved_Inventory_Is_Inconsistent()
    {
        var productId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(new OrderItemBuilder().ForProduct(productId).WithQuantity(2).Build()).Build();
        order.ConfirmPayment();
        var inventory = CreateReservedInventory(productId, 5, 1);

        var result = await CreateUseCase(order, [inventory]).ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.inventory_inconsistent", result.Error!.Code);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(5, inventory.Quantity);
        Assert.Equal(1, inventory.ReservedQuantity);
    }

    private static DispatchOrderUseCase CreateUseCase(
        Order? order,
        IReadOnlyList<InventoryItem> inventoryItems,
        ImmediateUnitOfWork? unitOfWork = null)
    {
        var orderRepository = new Mock<IOrderRepository>();
        orderRepository.Setup(value => value.GetByIdForUpdateAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        var inventoryRepository = new Mock<IInventoryItemRepository>();
        inventoryRepository.Setup(value => value.GetByProductIdsForUpdateAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItems);
        return new DispatchOrderUseCase(orderRepository.Object, inventoryRepository.Object, unitOfWork ?? new ImmediateUnitOfWork());
    }

    private static InventoryItem CreateReservedInventory(Guid productId, int quantity, int reservedQuantity) =>
        new InventoryItemBuilder().ForProduct(productId).WithQuantity(quantity).WithReservedQuantity(reservedQuantity).Build();
}
