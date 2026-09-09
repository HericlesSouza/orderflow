using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Enums;
using OrderFlow.UnitTests.Common.Builders;

namespace OrderFlow.UnitTests.Domain.Orders;

[Trait("Layer", "Domain")]
[Trait("Feature", "Orders")]
public sealed class OrderTests
{
    [Fact]
    public void Should_Create_Order_When_Items_Are_Valid()
    {
        var beforeCreation = DateTime.UtcNow;
        var orderItems = new List<OrderItem>
        {
            new OrderItemBuilder().WithQuantity(2).WithUnitPrice(100.00m).Build(),
            new OrderItemBuilder().WithQuantity(1).WithUnitPrice(50.00m).Build()
        };

        var order = Order.Create(orderItems);
        var afterCreation = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal(250.00m, order.TotalAmount);
        Assert.Equal(2, order.OrderItems.Count);
        Assert.Null(order.CancelledAt);
        Assert.Null(order.DispatchedAt);
        Assert.InRange(order.CreatedAt, beforeCreation, afterCreation);
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Creating_Order_Without_Items()
    {
        var act = () => Order.Create([]);

        var exception = Assert.Throws<ArgumentException>(act);

        Assert.Equal("Order must contain at least one item.", exception.Message);
    }

    [Fact]
    public void Should_Cancel_Order_When_Status_Is_PendingPayment()
    {
        var order = new OrderBuilder().Build();
        var beforeCancellation = DateTime.UtcNow;

        order.Cancel();

        var afterCancellation = DateTime.UtcNow;

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.NotNull(order.CancelledAt);
        Assert.InRange(order.CancelledAt!.Value, beforeCancellation, afterCancellation);
    }

    [Fact]
    public void Should_Throw_InvalidOperationException_When_Cancelling_An_Already_Cancelled_Order()
    {
        var order = new OrderBuilder().Build();
        order.Cancel();

        var act = () => order.Cancel();

        var exception = Assert.Throws<InvalidOperationException>(act);

        Assert.Equal("Order is already cancelled.", exception.Message);
    }

    [Fact]
    public void Should_Throw_InvalidOperationException_When_Cancelling_A_Confirmed_Order()
    {
        var order = new OrderBuilder().Build();
        order.ConfirmPayment();

        var act = () => order.Cancel();

        var exception = Assert.Throws<InvalidOperationException>(act);

        Assert.Equal("Confirmed order cannot be cancelled.", exception.Message);
    }

    [Fact]
    public void Should_Throw_InvalidOperationException_When_Cancelling_A_PaymentRejected_Order()
    {
        var order = new OrderBuilder().Build();
        order.RejectPayment();

        var act = () => order.Cancel();

        var exception = Assert.Throws<InvalidOperationException>(act);

        Assert.Equal("Payment rejected order cannot be cancelled.", exception.Message);
    }

    [Fact]
    public void Should_Throw_InvalidOperationException_When_Cancelling_A_Dispatched_Order()
    {
        var order = new OrderBuilder().Build();
        order.ConfirmPayment();
        order.Dispatch();

        var exception = Assert.Throws<InvalidOperationException>(order.Cancel);

        Assert.Equal("Dispatched order cannot be cancelled.", exception.Message);
    }

    [Fact]
    public void Should_Confirm_Payment_When_Order_Is_PendingPayment()
    {
        var order = new OrderBuilder().Build();

        order.ConfirmPayment();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Should_Reject_Payment_When_Order_Is_PendingPayment()
    {
        var order = new OrderBuilder().Build();

        order.RejectPayment();

        Assert.Equal(OrderStatus.PaymentRejected, order.Status);
    }

    [Fact]
    public void Should_Dispatch_Order_When_Status_Is_Confirmed()
    {
        var order = new OrderBuilder().Build();
        order.ConfirmPayment();
        var beforeDispatch = DateTime.UtcNow;

        order.Dispatch();

        var afterDispatch = DateTime.UtcNow;
        Assert.Equal(OrderStatus.Dispatched, order.Status);
        Assert.NotNull(order.DispatchedAt);
        Assert.InRange(order.DispatchedAt!.Value, beforeDispatch, afterDispatch);
    }

    [Theory]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    [InlineData(OrderStatus.Dispatched)]
    public void Should_Preserve_Order_When_Dispatching_A_Non_Confirmed_Order(OrderStatus status)
    {
        var order = new OrderBuilder().Build();
        switch (status)
        {
            case OrderStatus.Cancelled: order.Cancel(); break;
            case OrderStatus.PaymentRejected: order.RejectPayment(); break;
            case OrderStatus.Dispatched: order.ConfirmPayment(); order.Dispatch(); break;
        }
        var dispatchedAt = order.DispatchedAt;

        Assert.Throws<InvalidOperationException>(order.Dispatch);

        Assert.Equal(status, order.Status);
        Assert.Equal(dispatchedAt, order.DispatchedAt);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    [InlineData(OrderStatus.Dispatched)]
    public void Should_Preserve_Order_When_Rejecting_A_Non_Pending_Order(OrderStatus status)
    {
        var order = new OrderBuilder().Build();
        switch (status)
        {
            case OrderStatus.Confirmed: order.ConfirmPayment(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
            case OrderStatus.PaymentRejected: order.RejectPayment(); break;
            case OrderStatus.Dispatched: order.ConfirmPayment(); order.Dispatch(); break;
        }
        var cancelledAt = order.CancelledAt;

        Assert.Throws<InvalidOperationException>(() => order.RejectPayment());

        Assert.Equal(status, order.Status);
        Assert.Equal(cancelledAt, order.CancelledAt);
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    [InlineData(OrderStatus.Dispatched)]
    public void Should_Preserve_Order_When_Confirming_A_Non_Pending_Order(OrderStatus status)
    {
        var order = new OrderBuilder().Build();
        switch (status)
        {
            case OrderStatus.Confirmed: order.ConfirmPayment(); break;
            case OrderStatus.Cancelled: order.Cancel(); break;
            case OrderStatus.PaymentRejected: order.RejectPayment(); break;
            case OrderStatus.Dispatched: order.ConfirmPayment(); order.Dispatch(); break;
        }
        var cancelledAt = order.CancelledAt;
        var total = order.TotalAmount;

        Assert.Throws<InvalidOperationException>(() => order.ConfirmPayment());

        Assert.Equal(status, order.Status);
        Assert.Equal(cancelledAt, order.CancelledAt);
        Assert.Equal(total, order.TotalAmount);
    }
}
