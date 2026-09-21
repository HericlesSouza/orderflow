namespace OrderFlow.Application.Common.Outbox;

public sealed record PendingOutboxMessage(
    Guid Id,
    string EventType,
    string Payload);
