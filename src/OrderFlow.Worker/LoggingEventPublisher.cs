using OrderFlow.Application.Common.Outbox;

namespace OrderFlow.Worker;

internal sealed class LoggingEventPublisher(
    ILogger<LoggingEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(
        string eventType,
        string payload,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Publishing event {EventType}: {Payload}",
            eventType,
            payload);

        return Task.CompletedTask;
    }
}
