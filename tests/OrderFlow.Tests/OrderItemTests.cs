using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

public sealed class OrderItemTests
{
    [Fact]
    public void Should_Create_OrderItem_When_Input_Is_Valid()
    {
        var beforeCreation = DateTime.UtcNow;
        var productId = Guid.NewGuid();
        var orderItem = OrderItem.Create(productId, 2, 150.00m);
        var afterCreation = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, orderItem.Id);
        Assert.Equal(productId, orderItem.ProductId);
        Assert.Equal(2, orderItem.Quantity);
        Assert.Equal(150.00m, orderItem.UnitPrice);
        Assert.Equal(300.00m, orderItem.TotalPrice);
        Assert.InRange(orderItem.CreatedAt, beforeCreation, afterCreation);
        Assert.InRange(orderItem.UpdatedAt, beforeCreation, afterCreation);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_ProductId_Is_Empty()
    {
        var act = () => OrderItem.Create(Guid.Empty, 2, 150.00m);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Product ID cannot be empty.", exception.Message);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Quantity_Is_Zero()
    {
        var act = () => OrderItem.Create(Guid.NewGuid(), 0, 150.00m);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Quantity must be greater than zero.", exception.Message);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_UnitPrice_Is_Zero()
    {
        var act = () => OrderItem.Create(Guid.NewGuid(), 2, 0);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Unit price must be greater than zero.", exception.Message);
    }
}
