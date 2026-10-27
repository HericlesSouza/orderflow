namespace OrderFlow.Application.Common.Outbox;

public interface IOutboxRepository
{
    /// <summary>
    /// Stages an event for persistence in the current unit of work without committing it.
    /// </summary>
    void Add<TEvent>(string eventType, TEvent message) where TEvent : class;

    Task<IReadOnlyList<PendingOutboxMessage>> GetPendingAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task MarkProcessedAsync(
        Guid id,
        DateTime attemptedAt,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        Guid id,
        DateTime attemptedAt,
        string error,
        CancellationToken cancellationToken);
}
