using Microsoft.EntityFrameworkCore;
using OrderFlow.Application.Common.Outbox;

namespace OrderFlow.Infrastructure.Persistence.Outbox;

internal class OutboxMessageRepository(OrderFlowDbContext dbContext) : IOutboxMessageRepository
{
    public async Task<IReadOnlyList<PendingOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        return await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .Select(m => new PendingOutboxMessage(m.Id, m.EventType, m.Payload))
            .ToArrayAsync(cancellationToken);
    }

    public async Task MarkProcessedAsync(Guid id, DateTime attemptedAt, CancellationToken cancellationToken)
    {
        await dbContext.OutboxMessages
            .Where(m => m.Id == id && m.ProcessedAt == null)
            .ExecuteUpdateAsync(
                p => p
                    .SetProperty(m => m.ProcessedAt, attemptedAt)
                    .SetProperty(m => m.LastAttemptedAt, attemptedAt)
                    .SetProperty(m => m.AttemptCount, m => m.AttemptCount + 1)
                    .SetProperty(m => m.LastError, (string?)null),
                cancellationToken
            );
    }

    public async Task MarkFailedAsync(Guid id, DateTime attemptedAt, string error, CancellationToken cancellationToken)
    {
        await dbContext.OutboxMessages
            .Where(m => m.Id == id && m.ProcessedAt == null)
            .ExecuteUpdateAsync(
                properties => properties
                    .SetProperty(m => m.LastAttemptedAt, attemptedAt)
                    .SetProperty(m => m.AttemptCount, m => m.AttemptCount + 1)
                    .SetProperty(m => m.LastError, error),
                cancellationToken
            );
    }
}
