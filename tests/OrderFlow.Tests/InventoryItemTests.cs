using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

public sealed class InventoryItemTests
{
    [Fact]
    public void Should_Create_InventoryItem_When_Input_Is_Valid()
    {
        var beforeCreation = DateTime.UtcNow;
        var productId = Guid.NewGuid();
        var inventoryItem = InventoryItem.Create(productId, 10);
        var afterCreation = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, inventoryItem.Id);
        Assert.Equal(productId, inventoryItem.ProductId);
        Assert.Equal(10, inventoryItem.Quantity);
        Assert.InRange(inventoryItem.CreatedAt, beforeCreation, afterCreation);
        Assert.InRange(inventoryItem.UpdatedAt, beforeCreation, afterCreation);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_ProductId_Is_Empty()
    {
        var act = () => InventoryItem.Create(Guid.Empty, 10);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("InventoryItem product id cannot be empty.", exception.Message);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Quantity_Is_Negative()
    {
        var act = () => InventoryItem.Create(Guid.NewGuid(), -5);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("InventoryItem quantity cannot be negative.", exception.Message);
    }

    [Fact]
    public void Should_Update_Quantity_And_UpdatedAt_When_Quantity_Is_Valid()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        var beforeUpdate = DateTime.UtcNow;

        inventoryItem.UpdateQuantity(25);

        var afterUpdate = DateTime.UtcNow;

        Assert.Equal(25, inventoryItem.Quantity);
        Assert.InRange(inventoryItem.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Updating_Quantity_With_Negative_Value()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        var originalQuantity = inventoryItem.Quantity;
        var originalUpdatedAt = inventoryItem.UpdatedAt;
        var act = () => inventoryItem.UpdateQuantity(-1);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("InventoryItem quantity cannot be negative.", exception.Message);
        Assert.Equal(originalQuantity, inventoryItem.Quantity);
        Assert.Equal(originalUpdatedAt, inventoryItem.UpdatedAt);
    }
}
