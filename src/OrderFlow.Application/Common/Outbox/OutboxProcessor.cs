using Microsoft.Extensions.Logging;

namespace OrderFlow.Application.Common.Outbox;

public sealed class OutboxProcessor(
    IOutboxMessageRepository repository,
    IEventPublisher eventPublisher,
    ILogger<OutboxProcessor> logger
)
{
    public async Task ProcessPendingAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        var messages = await repository.GetPendingAsync(
            batchSize,
            cancellationToken);

        foreach (var message in messages)
        {
            var attemptedAt = DateTime.UtcNow;

            try
            {
                await eventPublisher.PublishAsync(
                    eventType: message.EventType,
                    payload: message.Payload,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var error = $"{ex.GetType().Name}: {ex.Message}";
                error = error[..Math.Min(error.Length, 2000)];

                await repository.MarkFailedAsync(message.Id, attemptedAt, error, cancellationToken);

                logger.LogError(ex, "Failed to process outbox message with ID {MessageId}", message.Id);
                continue;
            }

            await repository.MarkProcessedAsync(message.Id, attemptedAt, cancellationToken);
        }
    }
}
