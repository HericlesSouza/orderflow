namespace OrderFlow.Infrastructure.Persistence.Outbox;

internal sealed class OutboxMessage
{
    public Guid Id { get; private set; }
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? LastAttemptedAt { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    private OutboxMessage(string eventType, string payload)
    {
        Id = Guid.NewGuid();
        EventType = eventType;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
        AttemptCount = 0;
    }

    internal static OutboxMessage Create(string eventType, string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage(eventType, payload);
    }
}
