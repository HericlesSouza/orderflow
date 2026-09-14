using System.Text.Json;
using OrderFlow.Application.Common.Messaging;
using OrderFlow.Contracts.Events.Orders;

namespace OrderFlow.Infrastructure.Persistence.Outbox;

internal sealed class OutboxMessageWriter(OrderFlowDbContext dbContext) : IOutboxMessageWriter
{
    private const string OrderDispatchedEventType = "order.dispatched.v1";

    public void Add(OrderDispatched message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var payload = JsonSerializer.Serialize(message, JsonSerializerOptions.Web);
        var outboxMessage = OutboxMessage.Create(OrderDispatchedEventType, payload);

        dbContext.OutboxMessages.Add(outboxMessage);
    }
}
