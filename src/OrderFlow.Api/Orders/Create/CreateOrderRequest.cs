namespace OrderFlow.Api.Orders.Create;

public sealed record CreateOrderRequest(IReadOnlyCollection<CreateOrderItemRequest> Items);

public sealed record CreateOrderItemRequest(Guid ProductId, int Quantity);
