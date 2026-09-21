namespace OrderFlow.Application.Common.Outbox;

public interface IOutboxMessageRepository
{
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
