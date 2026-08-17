using Moq;
using OrderFlow.Application.InventoryItems.GetByProductId;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.UnitTests.Common.Builders;

namespace OrderFlow.UnitTests.Application.InventoryItems.GetByProductId;

[Trait("Layer", "Application")]
[Trait("Feature", "Inventory")]
public sealed class GetInventoryItemByProductIdUseCaseTests
{
    [Fact]
    public async Task Should_Return_Inventory_Item_When_Found()
    {
        // Arrange
        var inventoryItem = new InventoryItemBuilder().ForProduct(Guid.NewGuid()).WithQuantity(10).Build();

        var repository = new Mock<IInventoryItemRepository>();
        repository
            .Setup(r => r.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        var useCase = new GetInventoryItemByProductIdUseCase(repository.Object);

        // Act
        var result = await useCase.ExecuteAsync(inventoryItem.ProductId, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(inventoryItem.ProductId, result.Value!.ProductId);
        Assert.Equal(inventoryItem.Quantity, result.Value.Quantity);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Inventory_Item_Does_Not_Exist()
    {
        // Arrange
        var repository = new Mock<IInventoryItemRepository>();
        repository
            .Setup(r => r.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);
        var useCase = new GetInventoryItemByProductIdUseCase(repository.Object);

        // Act
        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("inventory_item.not_found", result.Error.Code);
        Assert.Equal("No inventory item found for the specified product.", result.Error.Message);
    }
}
