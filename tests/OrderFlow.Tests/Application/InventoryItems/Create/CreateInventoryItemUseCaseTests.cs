using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.InventoryItems;
using OrderFlow.Application.InventoryItems.Create;
using OrderFlow.Application.Products;
using OrderFlow.Domain.Entities;
using OrderFlow.Tests.Common.Fakes;

namespace OrderFlow.Tests;

public sealed class CreateInventoryItemUseCaseTests
{
    [Fact]
    public async Task Should_Create_Inventory_Item_When_Product_Exists()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var productRepository = new FakeProductRepository([product]);
        var inventoryRepository = new FakeInventoryItemRepository();
        var useCase = new CreateInventoryItemUseCase(inventoryRepository, productRepository);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, inventoryRepository.AddAsyncCallCount);
        Assert.Single(inventoryRepository.InventoryItems);
        Assert.Equal(10, result.Value!.Quantity);
        Assert.Equal(0, result.Value.ReservedQuantity);
        Assert.Equal(10, result.Value.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var productRepository = new FakeProductRepository();
        var inventoryRepository = new FakeInventoryItemRepository();
        var useCase = new CreateInventoryItemUseCase(inventoryRepository, productRepository);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(Guid.NewGuid(), 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.not_found", result.Error.Code);
        Assert.Equal(AppErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product not found.", result.Error.Message);
        Assert.Equal(0, inventoryRepository.AddAsyncCallCount);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Inventory_Item_Already_Exists_For_Product()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var existingInventoryItem = InventoryItem.Create(product.Id, 10);
        var productRepository = new FakeProductRepository([product]);
        var inventoryRepository = new FakeInventoryItemRepository([existingInventoryItem]);
        var useCase = new CreateInventoryItemUseCase(inventoryRepository, productRepository);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 5), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.already_exists", result.Error.Code);
        Assert.Equal(AppErrorType.Conflict, result.Error.Type);
        Assert.Equal("An inventory item already exists for this product.", result.Error.Message);
        Assert.Equal(0, inventoryRepository.AddAsyncCallCount);
    }

    [Fact]
    public async Task Should_Return_Validation_When_Inventory_Input_Is_Invalid()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var productRepository = new FakeProductRepository([product]);
        var inventoryRepository = new FakeInventoryItemRepository();
        var useCase = new CreateInventoryItemUseCase(inventoryRepository, productRepository);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, -1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal("InventoryItem quantity cannot be negative.", result.Error.Message);
        Assert.Equal(0, inventoryRepository.AddAsyncCallCount);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Duplicate_Is_Detected_During_Save()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var productRepository = new FakeProductRepository([product]);
        var inventoryRepository = new FakeInventoryItemRepository()
        {
            ThrowDuplicateOnAdd = true
        };
        var useCase = new CreateInventoryItemUseCase(inventoryRepository, productRepository);

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.already_exists", result.Error.Code);
        Assert.Equal(AppErrorType.Conflict, result.Error.Type);
        Assert.Equal("An inventory item already exists for this product.", result.Error.Message);
        Assert.Equal(1, inventoryRepository.AddAsyncCallCount);
    }
}
