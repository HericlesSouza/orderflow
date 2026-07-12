using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.InventoryItems.Increase;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests.Application.InventoryItems.Increase;

public sealed class IncreaseInventoryItemStockUseCaseTests
{
    [Fact]
    public async Task Should_Increase_Inventory_Stock_When_Item_Exists()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        inventoryItem.Reserve(4);

        var repositoryMock = new Mock<IInventoryItemRepository>();
        repositoryMock
            .Setup(repository => repository.GetByProductIdAsync(inventoryItem.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);
        repositoryMock
            .Setup(repository => repository.UpdateAsync(inventoryItem, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new IncreaseInventoryItemStockUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(
            new IncreaseInventoryItemStockInput(inventoryItem.ProductId, 5),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(15, result.Value!.Quantity);
        Assert.Equal(4, result.Value.ReservedQuantity);
        Assert.Equal(11, result.Value.AvailableQuantity);
        repositoryMock.Verify(
            repository => repository.UpdateAsync(inventoryItem, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Inventory_Item_Does_Not_Exist()
    {
        var repositoryMock = new Mock<IInventoryItemRepository>();
        repositoryMock
            .Setup(repository => repository.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var useCase = new IncreaseInventoryItemStockUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(
            new IncreaseInventoryItemStockInput(Guid.NewGuid(), 5),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.not_found", result.Error.Code);
        Assert.Equal(AppErrorType.NotFound, result.Error.Type);
        Assert.Equal("No inventory item found for the specified product.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Validation_When_Quantity_Is_Invalid()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);

        var repositoryMock = new Mock<IInventoryItemRepository>();
        repositoryMock
            .Setup(repository => repository.GetByProductIdAsync(inventoryItem.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        var useCase = new IncreaseInventoryItemStockUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(
            new IncreaseInventoryItemStockInput(inventoryItem.ProductId, 0),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal("Increase quantity must be greater than zero.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
