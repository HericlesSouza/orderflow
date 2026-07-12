namespace OrderFlow.Application.Orders.Create;

public sealed record CreateOrderInput(IReadOnlyCollection<CreateOrderItemInput> Items);

public sealed record CreateOrderItemInput(Guid ProductId, int Quantity);
