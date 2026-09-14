namespace OrderFlow.Contracts.Events.Orders;

public sealed record OrderDispatchedItem(
    Guid ProductId,
    int Quantity);
