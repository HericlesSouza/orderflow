using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Orders;
using OrderFlow.Application.Payments.Reject;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.UnitTests.Common.Builders;
using OrderFlow.UnitTests.Common.Fakes;

namespace OrderFlow.UnitTests.Application.Payments.Reject;

[Trait("Layer", "Application")]
[Trait("Feature", "Payments")]
public sealed class RejectPaymentUseCaseTests
{
    [Fact]
    public async Task Should_Reject_Order_And_Release_All_Reserved_Inventory_When_Pending()
    {
        var productId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(
            new OrderItemBuilder().ForProduct(productId).WithQuantity(2).WithUnitPrice(100).Build()).Build();
        var inventory = CreateReservedInventory(productId, 5, 2);
        var unitOfWork = new ImmediateUnitOfWork();
        var useCase = CreateUseCase(order, [inventory], unitOfWork);

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("PaymentRejected", result.Value!.Status);
        Assert.Equal(order.Id, result.Value.Id);
        Assert.Equal(order.TotalAmount, result.Value.TotalAmount);
        Assert.Equal(OrderStatus.PaymentRejected, order.Status);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(5, inventory.AvailableQuantity);
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
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    public async Task Should_Return_Conflict_Without_Releasing_Inventory_When_Order_Is_Not_Pending(OrderStatus status)
    {
        var productId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(
            new OrderItemBuilder().ForProduct(productId).WithQuantity(2).Build()).Build();
        var inventory = CreateReservedInventory(productId, 5, 2);
        switch (status)
        {
            case OrderStatus.Confirmed: order.ConfirmPayment(); break;
            case OrderStatus.Cancelled: order.Cancel(); inventory.Release(2); break;
            case OrderStatus.PaymentRejected: order.RejectPayment(); inventory.Release(2); break;
        }

        var result = await CreateUseCase(order, [inventory]).ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AppErrorType.Conflict, result.Error!.Type);
        Assert.Equal("payment.order_not_pending", result.Error.Code);
        Assert.Equal(status, order.Status);
        Assert.Equal(status == OrderStatus.Confirmed ? 2 : 0, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Reject_Order_When_Reserved_Inventory_Is_Inconsistent()
    {
        var productId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(
            new OrderItemBuilder().ForProduct(productId).WithQuantity(2).Build()).Build();
        var inventory = CreateReservedInventory(productId, 5, 1);

        var result = await CreateUseCase(order, [inventory]).ExecuteAsync(order.Id, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.inventory_inconsistent", result.Error!.Code);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(1, inventory.ReservedQuantity);
    }

    private static RejectPaymentUseCase CreateUseCase(
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
        return new RejectPaymentUseCase(orderRepository.Object, inventoryRepository.Object, unitOfWork ?? new ImmediateUnitOfWork());
    }

    private static InventoryItem CreateReservedInventory(Guid productId, int quantity, int reservedQuantity) =>
        new InventoryItemBuilder().ForProduct(productId).WithQuantity(quantity).WithReservedQuantity(reservedQuantity).Build();
}
