using OrderFlow.Domain.Entities;
using OrderFlow.UnitTests.Common.Builders;

namespace OrderFlow.UnitTests.Domain.Inventory;

[Trait("Layer", "Domain")]
[Trait("Feature", "Inventory")]
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

    public static TheoryData<Guid, int, string> InvalidInventoryItems => new()
    {
        { Guid.Empty, 10, "InventoryItem product id cannot be empty." },
        { Guid.Parse("11111111-1111-1111-1111-111111111111"), -5, "InventoryItem quantity cannot be negative." }
    };

    [Theory]
    [MemberData(nameof(InvalidInventoryItems))]
    public void Should_Throw_ArgumentException_When_Inventory_Input_Is_Invalid(
        Guid productId,
        int quantity,
        string expectedMessage)
    {
        var act = () => InventoryItem.Create(productId, quantity);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Should_Increase_Stock_And_Update_UpdatedAt_When_Quantity_Is_Valid()
    {
        var inventoryItem = new InventoryItemBuilder().WithQuantity(10).Build();
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
        var inventoryItem = new InventoryItemBuilder().WithQuantity(10).Build();
        var originalQuantity = inventoryItem.Quantity;
        var originalUpdatedAt = inventoryItem.UpdatedAt;
        var act = () => inventoryItem.IncreaseStock(0);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Increase quantity must be greater than zero.", exception.Message);
        Assert.Equal(originalQuantity, inventoryItem.Quantity);
        Assert.Equal(originalUpdatedAt, inventoryItem.UpdatedAt);
    }

    [Fact]
    public void Should_Reserve_Quantity_And_Update_Available_Quantity_When_Stock_Is_Sufficient()
    {
        var inventoryItem = new InventoryItemBuilder().WithQuantity(10).Build();
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
        var inventoryItem = new InventoryItemBuilder().WithQuantity(10).Build();
        var originalReservedQuantity = inventoryItem.ReservedQuantity;
        var originalUpdatedAt = inventoryItem.UpdatedAt;
        var act = () => inventoryItem.Reserve(11);

        var exception = Assert.Throws<InvalidOperationException>(act);

        Assert.Equal("Not enough available quantity to reserve.", exception.Message);
        Assert.Equal(originalReservedQuantity, inventoryItem.ReservedQuantity);
        Assert.Equal(originalUpdatedAt, inventoryItem.UpdatedAt);
    }

    [Fact]
    public void Should_Release_Reserved_Quantity_And_Update_Available_Quantity_When_Reservation_Is_Sufficient()
    {
        var inventoryItem = new InventoryItemBuilder().WithQuantity(10).Build();
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
        var inventoryItem = new InventoryItemBuilder().WithQuantity(10).Build();
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
