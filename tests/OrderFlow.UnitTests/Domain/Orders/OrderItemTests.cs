using OrderFlow.Domain.Entities;

namespace OrderFlow.UnitTests.Domain.Orders;

[Trait("Layer", "Domain")]
[Trait("Feature", "Orders")]
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

    public static TheoryData<Guid, int, decimal, string> InvalidOrderItems => new()
    {
        { Guid.Empty, 2, 150.00m, "Product ID cannot be empty." },
        { Guid.Parse("22222222-2222-2222-2222-222222222222"), 0, 150.00m, "Quantity must be greater than zero." },
        { Guid.Parse("22222222-2222-2222-2222-222222222222"), 2, 0, "Unit price must be greater than zero." }
    };

    [Theory]
    [MemberData(nameof(InvalidOrderItems))]
    public void Should_Throw_ArgumentException_When_Order_Item_Input_Is_Invalid(
        Guid productId,
        int quantity,
        decimal unitPrice,
        string expectedMessage)
    {
        var act = () => OrderItem.Create(productId, quantity, unitPrice);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal(expectedMessage, exception.Message);
    }
}
