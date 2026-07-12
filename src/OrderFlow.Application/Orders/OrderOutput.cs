using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.Orders;

public sealed record OrderOutput(
    Guid Id,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    IReadOnlyCollection<OrderItemOutput> Items)
{
    public static OrderOutput FromEntity(Order order)
    {
        return new OrderOutput(
            order.Id,
            order.Status.ToString(),
            order.TotalAmount,
            order.CreatedAt,
            order.CancelledAt,
            order.OrderItems.Select(OrderItemOutput.FromEntity).ToArray());
    }
}

public sealed record OrderItemOutput(
    Guid Id,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice)
{
    public static OrderItemOutput FromEntity(OrderItem item)
    {
        return new OrderItemOutput(
            item.Id,
            item.ProductId,
            item.Quantity,
            item.UnitPrice,
            item.TotalPrice);
    }
}
