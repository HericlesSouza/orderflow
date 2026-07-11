using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.InventoryItems;
using OrderFlow.Application.InventoryItems.Create;
using OrderFlow.Application.Repositories;
using OrderFlow.Application.Products;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests.Application.InventoryItems.Create;

public sealed class CreateInventoryItemUseCaseTests
{
    [Fact]
    public async Task Should_Create_Inventory_Item_When_Product_Exists()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        InventoryItem? savedInventoryItem = null;
        var productRepositoryMock = new Mock<IProductRepository>();
        productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var inventoryRepositoryMock = new Mock<IInventoryItemRepository>();
        inventoryRepositoryMock
            .Setup(repository => repository.ExistsByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        inventoryRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .Callback<InventoryItem, CancellationToken>((inventoryItem, _) => savedInventoryItem = inventoryItem)
            .Returns(Task.CompletedTask);

        var useCase = new CreateInventoryItemUseCase(inventoryRepositoryMock.Object, productRepositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(savedInventoryItem);
        Assert.Equal(10, result.Value!.Quantity);
        Assert.Equal(0, result.Value.ReservedQuantity);
        Assert.Equal(10, result.Value.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var productRepositoryMock = new Mock<IProductRepository>();
        productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var inventoryRepositoryMock = new Mock<IInventoryItemRepository>();
        var useCase = new CreateInventoryItemUseCase(inventoryRepositoryMock.Object, productRepositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(Guid.NewGuid(), 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.not_found", result.Error.Code);
        Assert.Equal(AppErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product not found.", result.Error.Message);
        inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Inventory_Item_Already_Exists_For_Product()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var productRepositoryMock = new Mock<IProductRepository>();
        productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var inventoryRepositoryMock = new Mock<IInventoryItemRepository>();
        inventoryRepositoryMock
            .Setup(repository => repository.ExistsByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var useCase = new CreateInventoryItemUseCase(inventoryRepositoryMock.Object, productRepositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 5), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.already_exists", result.Error.Code);
        Assert.Equal(AppErrorType.Conflict, result.Error.Type);
        Assert.Equal("An inventory item already exists for this product.", result.Error.Message);
        inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Validation_When_Inventory_Input_Is_Invalid()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var productRepositoryMock = new Mock<IProductRepository>();
        productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var inventoryRepositoryMock = new Mock<IInventoryItemRepository>();
        inventoryRepositoryMock
            .Setup(repository => repository.ExistsByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var useCase = new CreateInventoryItemUseCase(inventoryRepositoryMock.Object, productRepositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, -1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal("InventoryItem quantity cannot be negative.", result.Error.Message);
        inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Duplicate_Is_Detected_During_Save()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var productRepositoryMock = new Mock<IProductRepository>();
        productRepositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var inventoryRepositoryMock = new Mock<IInventoryItemRepository>();
        inventoryRepositoryMock
            .Setup(repository => repository.ExistsByProductIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        inventoryRepositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DuplicateInventoryItemException(product.Id));

        var useCase = new CreateInventoryItemUseCase(inventoryRepositoryMock.Object, productRepositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.already_exists", result.Error.Code);
        Assert.Equal(AppErrorType.Conflict, result.Error.Type);
        Assert.Equal("An inventory item already exists for this product.", result.Error.Message);
        inventoryRepositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
