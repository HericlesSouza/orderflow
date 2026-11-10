namespace OrderFlow.Application.Common.Outbox;

public interface IEventPublisher
{
    Task PublishAsync(Guid messageId, string eventType, string payload, CancellationToken cancellationToken);
}
