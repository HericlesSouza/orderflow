namespace OrderFlow.Contracts.Events.Orders;

public sealed record OrderDispatched(
    Guid OrderId,
    DateTime DispatchedAtUtc,
    IReadOnlyCollection<OrderDispatchedItem> Items);
