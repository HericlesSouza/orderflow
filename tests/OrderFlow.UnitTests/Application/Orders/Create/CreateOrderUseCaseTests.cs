using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Orders;
using OrderFlow.Application.Orders.Create;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.UnitTests.Common.Builders;
using OrderFlow.UnitTests.Common.Fakes;

namespace OrderFlow.UnitTests.Application.Orders.Create;

[Trait("Layer", "Application")]
[Trait("Feature", "Orders")]
public sealed class CreateOrderUseCaseTests
{
    [Fact]
    public async Task Should_Create_Order_And_Reserve_Inventory_When_Input_Is_Valid()
    {
        var firstProduct = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var secondProduct = new ProductBuilder().WithName("Mouse").WithDescription("Wireless mouse").WithPrice(150.00m).Build();
        var firstInventory = new InventoryItemBuilder().ForProduct(firstProduct.Id).WithQuantity(5).Build();
        var secondInventory = new InventoryItemBuilder().ForProduct(secondProduct.Id).WithQuantity(10).Build();
        Order? savedOrder = null;

        var productRepository = CreateProductRepository(firstProduct, secondProduct);
        var inventoryRepository = CreateInventoryRepository(firstInventory, secondInventory);
        var orderRepository = new Mock<IOrderRepository>();
        orderRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => savedOrder = order)
            .Returns(Task.CompletedTask);
        var unitOfWork = new ImmediateUnitOfWork();
        var fixture = new CreateOrderUseCaseFixture(
            productRepository,
            inventoryRepository,
            orderRepository,
            unitOfWork);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([
                new CreateOrderItemInput(firstProduct.Id, 2),
                new CreateOrderItemInput(secondProduct.Id, 3)
            ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(savedOrder);
        Assert.Equal(7450.00m, result.Value!.TotalAmount);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, firstInventory.ReservedQuantity);
        Assert.Equal(3, secondInventory.ReservedQuantity);
        Assert.Contains(result.Value.Items, item => item.ProductId == firstProduct.Id && item.UnitPrice == 3500.00m);
        Assert.Equal(1, unitOfWork.ExecutionCount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_A_Product_Does_Not_Exist()
    {
        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var fixture = new CreateOrderUseCaseFixture(productRepository: productRepository);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([new CreateOrderItemInput(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        AssertFailure(result, "order.product_not_found", AppErrorType.NotFound);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_A_Product_Is_Inactive()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        product.Deactivate();
        var fixture = new CreateOrderUseCaseFixture(productRepository: CreateProductRepository(product));
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([new CreateOrderItemInput(product.Id, 1)]),
            CancellationToken.None);

        AssertFailure(result, "order.product_inactive", AppErrorType.Conflict);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Inventory_Does_Not_Exist()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var fixture = new CreateOrderUseCaseFixture(
            productRepository: CreateProductRepository(product),
            inventoryRepository: CreateInventoryRepository());
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([new CreateOrderItemInput(product.Id, 1)]),
            CancellationToken.None);

        AssertFailure(result, "order.inventory_not_found", AppErrorType.Conflict);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Save_Order_When_Inventory_Is_Insufficient()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var inventory = new InventoryItemBuilder().ForProduct(product.Id).WithQuantity(1).Build();
        var orderRepository = new Mock<IOrderRepository>();
        var fixture = new CreateOrderUseCaseFixture(
            productRepository: CreateProductRepository(product),
            inventoryRepository: CreateInventoryRepository(inventory),
            orderRepository: orderRepository);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([new CreateOrderItemInput(product.Id, 2)]),
            CancellationToken.None);

        AssertFailure(result, "order.insufficient_inventory", AppErrorType.Conflict);
        Assert.Equal(0, inventory.ReservedQuantity);
        orderRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Not_Reserve_Any_Inventory_When_One_Item_Has_Insufficient_Inventory()
    {
        var products = new[]
        {
            new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build(),
            new ProductBuilder().WithName("Mouse").WithDescription("Wireless mouse").WithPrice(150.00m).Build()
        }.OrderBy(product => product.Id).ToArray();
        var sufficientInventory = new InventoryItemBuilder().ForProduct(products[0].Id).WithQuantity(5).Build();
        var insufficientInventory = new InventoryItemBuilder().ForProduct(products[1].Id).WithQuantity(1).Build();
        var orderRepository = new Mock<IOrderRepository>();
        var fixture = new CreateOrderUseCaseFixture(
            productRepository: CreateProductRepository(products),
            inventoryRepository: CreateInventoryRepository(sufficientInventory, insufficientInventory),
            orderRepository: orderRepository);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([
                new CreateOrderItemInput(products[0].Id, 2),
                new CreateOrderItemInput(products[1].Id, 2)
            ]),
            CancellationToken.None);

        AssertFailure(result, "order.insufficient_inventory", AppErrorType.Conflict);
        Assert.Equal(0, sufficientInventory.ReservedQuantity);
        Assert.Equal(0, insufficientInventory.ReservedQuantity);
        orderRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed class CreateOrderUseCaseFixture(
        Mock<IProductRepository>? productRepository = null,
        Mock<IInventoryItemRepository>? inventoryRepository = null,
        Mock<IOrderRepository>? orderRepository = null,
        ImmediateUnitOfWork? unitOfWork = null)
    {
        public CreateOrderUseCase CreateSut()
        {
            return new CreateOrderUseCase(
                (productRepository ?? new Mock<IProductRepository>()).Object,
                (inventoryRepository ?? new Mock<IInventoryItemRepository>()).Object,
                (orderRepository ?? new Mock<IOrderRepository>()).Object,
                unitOfWork ?? new ImmediateUnitOfWork());
        }
    }

    private static Mock<IProductRepository> CreateProductRepository(params Product[] products)
    {
        var repository = new Mock<IProductRepository>();
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);
        return repository;
    }

    private static Mock<IInventoryItemRepository> CreateInventoryRepository(params InventoryItem[] inventoryItems)
    {
        var repository = new Mock<IInventoryItemRepository>();
        repository
            .Setup(value => value.GetByProductIdsForUpdateAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItems);
        return repository;
    }

    private static void AssertFailure(Result<OrderOutput> result, string code, AppErrorType type)
    {
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal(code, result.Error.Code);
        Assert.Equal(type, result.Error.Type);
    }

}
