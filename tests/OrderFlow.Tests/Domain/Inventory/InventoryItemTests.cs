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
        Assert.Equal(0, inventoryItem.ReservedQuantity);
        Assert.Equal(10, inventoryItem.AvailableQuantity);
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
    public void Should_Increase_Stock_And_Update_UpdatedAt_When_Quantity_Is_Valid()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        var beforeUpdate = DateTime.UtcNow;

        inventoryItem.IncreaseStock(5);

        var afterUpdate = DateTime.UtcNow;

        Assert.Equal(15, inventoryItem.Quantity);
        Assert.Equal(15, inventoryItem.AvailableQuantity);
        Assert.InRange(inventoryItem.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Increasing_Stock_With_Zero_Quantity()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        var originalQuantity = inventoryItem.Quantity;
        var originalUpdatedAt = inventoryItem.UpdatedAt;
        var act = () => inventoryItem.IncreaseStock(0);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Increase quantity must be greater than zero.", exception.Message);
        Assert.Equal(originalQuantity, inventoryItem.Quantity);
        Assert.Equal(originalUpdatedAt, inventoryItem.UpdatedAt);
    }

    [Fact]
    public void Should_Reserve_Quantity_And_Update_AvailableQuantity()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        var beforeUpdate = DateTime.UtcNow;

        inventoryItem.Reserve(4);

        var afterUpdate = DateTime.UtcNow;

        Assert.Equal(10, inventoryItem.Quantity);
        Assert.Equal(4, inventoryItem.ReservedQuantity);
        Assert.Equal(6, inventoryItem.AvailableQuantity);
        Assert.InRange(inventoryItem.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Throw_InvalidOperationException_When_Reserve_Exceeds_AvailableQuantity()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        var originalReservedQuantity = inventoryItem.ReservedQuantity;
        var originalUpdatedAt = inventoryItem.UpdatedAt;
        var act = () => inventoryItem.Reserve(11);

        var exception = Assert.Throws<InvalidOperationException>(act);

        Assert.Equal("Not enough available quantity to reserve.", exception.Message);
        Assert.Equal(originalReservedQuantity, inventoryItem.ReservedQuantity);
        Assert.Equal(originalUpdatedAt, inventoryItem.UpdatedAt);
    }

    [Fact]
    public void Should_Release_Reserved_Quantity_And_Update_AvailableQuantity()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        inventoryItem.Reserve(4);
        var beforeUpdate = DateTime.UtcNow;

        inventoryItem.Release(3);

        var afterUpdate = DateTime.UtcNow;

        Assert.Equal(10, inventoryItem.Quantity);
        Assert.Equal(1, inventoryItem.ReservedQuantity);
        Assert.Equal(9, inventoryItem.AvailableQuantity);
        Assert.InRange(inventoryItem.UpdatedAt, beforeUpdate, afterUpdate);
    }

    [Fact]
    public void Should_Throw_InvalidOperationException_When_Releasing_More_Than_ReservedQuantity()
    {
        var inventoryItem = InventoryItem.Create(Guid.NewGuid(), 10);
        inventoryItem.Reserve(2);
        var originalReservedQuantity = inventoryItem.ReservedQuantity;
        var originalUpdatedAt = inventoryItem.UpdatedAt;
        var act = () => inventoryItem.Release(3);

        var exception = Assert.Throws<InvalidOperationException>(act);

        Assert.Equal("Not enough reserved quantity to release.", exception.Message);
        Assert.Equal(originalReservedQuantity, inventoryItem.ReservedQuantity);
        Assert.Equal(originalUpdatedAt, inventoryItem.UpdatedAt);
    }
}
