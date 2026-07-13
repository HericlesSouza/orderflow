using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Application.Common.Transactions;
using OrderFlow.Application.Orders;
using OrderFlow.Application.Orders.Create;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests.Application.Orders.Create;

public sealed class CreateOrderUseCaseTests
{
    [Fact]
    public async Task Should_Create_Order_And_Reserve_Inventory_When_Input_Is_Valid()
    {
        var firstProduct = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var secondProduct = Product.Create("Mouse", 150.00m, "Wireless mouse");
        var firstInventory = InventoryItem.Create(firstProduct.Id, 5);
        var secondInventory = InventoryItem.Create(secondProduct.Id, 10);
        Order? savedOrder = null;

        var productRepository = CreateProductRepository(firstProduct, secondProduct);
        var inventoryRepository = CreateInventoryRepository(firstInventory, secondInventory);
        var orderRepository = new Mock<IOrderRepository>();
        orderRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => savedOrder = order)
            .Returns(Task.CompletedTask);
        var unitOfWork = new ImmediateUnitOfWork();
        var useCase = new CreateOrderUseCase(
            productRepository.Object,
            inventoryRepository.Object,
            orderRepository.Object,
            unitOfWork);

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
        var useCase = CreateUseCase(productRepository: productRepository);

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([new CreateOrderItemInput(Guid.NewGuid(), 1)]),
            CancellationToken.None);

        AssertFailure(result, "order.product_not_found", AppErrorType.NotFound);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_A_Product_Is_Inactive()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        product.Deactivate();
        var useCase = CreateUseCase(productRepository: CreateProductRepository(product));

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([new CreateOrderItemInput(product.Id, 1)]),
            CancellationToken.None);

        AssertFailure(result, "order.product_inactive", AppErrorType.Conflict);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Inventory_Does_Not_Exist()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var useCase = CreateUseCase(
            productRepository: CreateProductRepository(product),
            inventoryRepository: CreateInventoryRepository());

        var result = await useCase.ExecuteAsync(
            new CreateOrderInput([new CreateOrderItemInput(product.Id, 1)]),
            CancellationToken.None);

        AssertFailure(result, "order.inventory_not_found", AppErrorType.Conflict);
    }

    [Fact]
    public async Task Should_Return_Conflict_And_Not_Save_Order_When_Inventory_Is_Insufficient()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var inventory = InventoryItem.Create(product.Id, 1);
        var orderRepository = new Mock<IOrderRepository>();
        var useCase = CreateUseCase(
            productRepository: CreateProductRepository(product),
            inventoryRepository: CreateInventoryRepository(inventory),
            orderRepository: orderRepository);

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
            Product.Create("Notebook", 3500.00m, "Gaming notebook"),
            Product.Create("Mouse", 150.00m, "Wireless mouse")
        }.OrderBy(product => product.Id).ToArray();
        var sufficientInventory = InventoryItem.Create(products[0].Id, 5);
        var insufficientInventory = InventoryItem.Create(products[1].Id, 1);
        var orderRepository = new Mock<IOrderRepository>();
        var useCase = CreateUseCase(
            productRepository: CreateProductRepository(products),
            inventoryRepository: CreateInventoryRepository(sufficientInventory, insufficientInventory),
            orderRepository: orderRepository);

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

    private static CreateOrderUseCase CreateUseCase(
        Mock<IProductRepository>? productRepository = null,
        Mock<IInventoryItemRepository>? inventoryRepository = null,
        Mock<IOrderRepository>? orderRepository = null,
        ImmediateUnitOfWork? unitOfWork = null)
    {
        return new CreateOrderUseCase(
            (productRepository ?? new Mock<IProductRepository>()).Object,
            (inventoryRepository ?? new Mock<IInventoryItemRepository>()).Object,
            (orderRepository ?? new Mock<IOrderRepository>()).Object,
            unitOfWork ?? new ImmediateUnitOfWork());
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
