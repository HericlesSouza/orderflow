using OrderFlow.Contracts.Events.Orders;

namespace OrderFlow.Application.Common.Messaging;

public interface IOutboxMessageWriter
{
    void Add(OrderDispatched message);
}
