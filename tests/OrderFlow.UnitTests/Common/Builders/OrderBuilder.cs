using OrderFlow.Domain.Entities;

namespace OrderFlow.UnitTests.Common.Builders;

internal sealed class OrderBuilder
{
    private IReadOnlyCollection<OrderItem> _items = [new OrderItemBuilder().Build()];

    public OrderBuilder WithItems(params OrderItem[] items)
    {
        _items = items;
        return this;
    }

    public Order Build()
    {
        return Order.Create(_items);
    }
}
