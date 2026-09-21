using OrderFlow.Contracts.Events.Orders;

namespace OrderFlow.Application.Common.Outbox;

public interface IOutboxMessageWriter
{
    void Add(OrderDispatched message);
}
