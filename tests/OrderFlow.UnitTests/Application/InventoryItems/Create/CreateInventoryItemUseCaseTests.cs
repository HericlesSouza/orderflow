using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.InventoryItems;
using OrderFlow.Application.InventoryItems.Create;
using OrderFlow.Application.Products;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.UnitTests.Common.Builders;

namespace OrderFlow.UnitTests.Application.InventoryItems.Create;

[Trait("Layer", "Application")]
[Trait("Feature", "Inventory")]
public sealed class CreateInventoryItemUseCaseTests
{
    [Fact]
    public async Task Should_Create_Inventory_Item_When_Product_Exists()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var fixture = new CreateInventoryItemUseCaseFixture();
        fixture.ProductReturns(product);
        fixture.InventoryExists(false);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(fixture.SavedInventoryItem);
        Assert.Equal(10, result.Value!.Quantity);
        Assert.Equal(0, result.Value.ReservedQuantity);
        Assert.Equal(10, result.Value.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var fixture = new CreateInventoryItemUseCaseFixture();
        fixture.ProductReturns(null);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(Guid.NewGuid(), 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.not_found", result.Error.Code);
        Assert.Equal(AppErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product not found.", result.Error.Message);
        fixture.InventoryRepository.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Inventory_Item_Already_Exists_For_Product()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var fixture = new CreateInventoryItemUseCaseFixture();
        fixture.ProductReturns(product);
        fixture.InventoryExists(true);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 5), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.already_exists", result.Error.Code);
        Assert.Equal(AppErrorType.Conflict, result.Error.Type);
        Assert.Equal("An inventory item already exists for this product.", result.Error.Message);
        fixture.InventoryRepository.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Validation_When_Inventory_Input_Is_Invalid()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var fixture = new CreateInventoryItemUseCaseFixture();
        fixture.ProductReturns(product);
        fixture.InventoryExists(false);
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, -1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal("InventoryItem quantity cannot be negative.", result.Error.Message);
        fixture.InventoryRepository.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Duplicate_Is_Detected_During_Save()
    {
        var product = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var fixture = new CreateInventoryItemUseCaseFixture();
        fixture.ProductReturns(product);
        fixture.InventoryExists(false);
        fixture.SaveThrows(new DuplicateInventoryItemException(product.Id));
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(new CreateInventoryItemInput(product.Id, 10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.already_exists", result.Error.Code);
        Assert.Equal(AppErrorType.Conflict, result.Error.Type);
        Assert.Equal("An inventory item already exists for this product.", result.Error.Message);
        fixture.InventoryRepository.Verify(
            repository => repository.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
    private sealed class CreateInventoryItemUseCaseFixture
    {
        public CreateInventoryItemUseCaseFixture()
        {
            InventoryRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<InventoryItem>(),
                    It.IsAny<CancellationToken>()))
                .Callback<InventoryItem, CancellationToken>(
                    (inventoryItem, _) => SavedInventoryItem = inventoryItem)
                .Returns(Task.CompletedTask);
        }

        public Mock<IProductRepository> ProductRepository { get; } = new();
        public Mock<IInventoryItemRepository> InventoryRepository { get; } = new();
        public InventoryItem? SavedInventoryItem { get; private set; }

        public void ProductReturns(Product? product)
        {
            ProductRepository
                .Setup(repository => repository.GetByIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
        }

        public void InventoryExists(bool exists)
        {
            InventoryRepository
                .Setup(repository => repository.ExistsByProductIdAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(exists);
        }

        public void SaveThrows(Exception exception)
        {
            InventoryRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<InventoryItem>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);
        }

        public CreateInventoryItemUseCase CreateSut()
        {
            return new CreateInventoryItemUseCase(
                InventoryRepository.Object,
                ProductRepository.Object);
        }
    }
}
