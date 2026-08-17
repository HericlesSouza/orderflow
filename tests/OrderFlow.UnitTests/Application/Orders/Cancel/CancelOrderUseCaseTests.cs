using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Orders;
using OrderFlow.Application.Orders.Cancel;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.UnitTests.Common.Builders;
using OrderFlow.UnitTests.Common.Fakes;

namespace OrderFlow.UnitTests.Application.Orders.Cancel;

[Trait("Layer", "Application")]
[Trait("Feature", "Orders")]
public sealed class CancelOrderUseCaseTests
{
    [Fact]
    public async Task Should_Cancel_Order_And_Release_All_Reserved_Inventory_When_Order_Is_PendingPayment()
    {
        var firstProductId = Guid.NewGuid();
        var secondProductId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(
            new OrderItemBuilder().ForProduct(firstProductId).WithQuantity(2).WithUnitPrice(100.00m).Build(),
            new OrderItemBuilder().ForProduct(secondProductId).WithQuantity(3).WithUnitPrice(50.00m).Build()).Build();
        var firstInventory = CreateReservedInventory(firstProductId, 5, 2);
        var secondInventory = CreateReservedInventory(secondProductId, 10, 3);
        var unitOfWork = new ImmediateUnitOfWork();
        var fixture = new CancelOrderUseCaseFixture(order, [firstInventory, secondInventory], unitOfWork);
        var useCase = fixture.CreateSut();

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
        var fixture = new CancelOrderUseCaseFixture(order: null, inventoryItems: []);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        AssertFailure(result, "order.not_found", AppErrorType.NotFound);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Release_Inventory_When_Order_Is_Already_Cancelled()
    {
        var (order, inventory) = CreateOrderWithReservedInventory();
        order.Cancel();
        inventory.Release(2);
        var fixture = new CancelOrderUseCaseFixture(order, [inventory]);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.cannot_cancel", AppErrorType.Conflict);
        Assert.Equal("Order is already cancelled.", result.Error!.Message);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(5, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Release_Inventory_When_Order_Is_Confirmed()
    {
        var (order, inventory) = CreateOrderWithReservedInventory();
        order.ConfirmPayment();
        var fixture = new CancelOrderUseCaseFixture(order, [inventory]);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.cannot_cancel", AppErrorType.Conflict);
        Assert.Equal(2, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Release_Inventory_When_Order_Is_PaymentRejected()
    {
        var (order, inventory) = CreateOrderWithReservedInventory();
        order.RejectPayment();
        var fixture = new CancelOrderUseCaseFixture(order, [inventory]);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.cannot_cancel", AppErrorType.Conflict);
        Assert.Equal(2, inventory.ReservedQuantity);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Cancel_Order_When_Inventory_Does_Not_Exist()
    {
        var (order, _) = CreateOrderWithReservedInventory();
        var fixture = new CancelOrderUseCaseFixture(order, []);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.inventory_inconsistent", AppErrorType.Conflict);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Null(order.CancelledAt);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Cancel_Order_When_Reserved_Inventory_Is_Insufficient()
    {
        var productId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(
            new OrderItemBuilder().ForProduct(productId).WithQuantity(2).WithUnitPrice(100.00m).Build()).Build();
        var inventory = CreateReservedInventory(productId, 5, 1);
        var fixture = new CancelOrderUseCaseFixture(order, [inventory]);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(order.Id, CancellationToken.None);

        AssertFailure(result, "order.inventory_inconsistent", AppErrorType.Conflict);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(1, inventory.ReservedQuantity);
    }

    private sealed class CancelOrderUseCaseFixture(
        Order? order,
        IReadOnlyList<InventoryItem> inventoryItems,
        ImmediateUnitOfWork? unitOfWork = null)
    {
        public ImmediateUnitOfWork UnitOfWork { get; } = unitOfWork ?? new ImmediateUnitOfWork();

        public CancelOrderUseCase CreateSut()
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
                UnitOfWork);
        }
    }

    private static (Order Order, InventoryItem Inventory) CreateOrderWithReservedInventory()
    {
        var productId = Guid.NewGuid();
        var order = new OrderBuilder().WithItems(
            new OrderItemBuilder().ForProduct(productId).WithQuantity(2).WithUnitPrice(100.00m).Build()).Build();
        var inventory = CreateReservedInventory(productId, 5, 2);

        return (order, inventory);
    }

    private static InventoryItem CreateReservedInventory(Guid productId, int quantity, int reservedQuantity)
    {
        var inventory = new InventoryItemBuilder()
            .ForProduct(productId)
            .WithQuantity(quantity)
            .WithReservedQuantity(reservedQuantity)
            .Build();
        return inventory;
    }

    private static void AssertFailure(Result<OrderOutput> result, string code, AppErrorType type)
    {
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(type, result.Error.Type);
    }

}
