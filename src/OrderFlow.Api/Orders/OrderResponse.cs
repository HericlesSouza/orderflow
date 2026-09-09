using OrderFlow.Application.Orders;

namespace OrderFlow.Api.Orders;

public sealed record OrderResponse(
    Guid Id,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    DateTime? CancelledAt,
    DateTime? DispatchedAt,
    IReadOnlyCollection<OrderItemResponse> Items)
{
    public static OrderResponse FromOutput(OrderOutput output)
    {
        return new OrderResponse(
            output.Id,
            output.Status,
            output.TotalAmount,
            output.CreatedAt,
            output.CancelledAt,
            output.DispatchedAt,
            output.Items.Select(OrderItemResponse.FromOutput).ToArray());
    }
}

public sealed record OrderItemResponse(
    Guid Id,
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice)
{
    public static OrderItemResponse FromOutput(OrderItemOutput output)
    {
        return new OrderItemResponse(
            output.Id,
            output.ProductId,
            output.Quantity,
            output.UnitPrice,
            output.TotalPrice);
    }
}
