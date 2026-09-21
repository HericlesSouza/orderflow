namespace OrderFlow.Application.Common.Outbox;

public interface IEventPublisher
{
    Task PublishAsync(string eventType, string payload, CancellationToken cancellationToken);
}
