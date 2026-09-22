using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderFlow.Application.Common.Outbox;

namespace OrderFlow.UnitTests.Application.Common.Outbox;

internal sealed class OutboxProcessorFixture : IDisposable
{
    public const int BatchSize = 20;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly OutboxProcessor _processor;

    public OutboxProcessorFixture(params PendingOutboxMessage[] messages)
    {
        Repository
            .Setup(value => value.GetPendingAsync(It.IsAny<int>(), CancellationToken))
            .ReturnsAsync(messages);
        EventPublisher
            .Setup(value => value.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken))
            .Returns(Task.CompletedTask);
        Repository
            .Setup(value => value.MarkProcessedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                CancellationToken))
            .Returns(Task.CompletedTask);
        Repository
            .Setup(value => value.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                CancellationToken))
            .Returns(Task.CompletedTask);

        _processor = new OutboxProcessor(
            Repository.Object,
            EventPublisher.Object,
            NullLogger<OutboxProcessor>.Instance);
    }

    public Mock<IOutboxMessageRepository> Repository { get; } = new();
    public Mock<IEventPublisher> EventPublisher { get; } = new();
    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    public Task ProcessPendingAsync(int batchSize = BatchSize)
    {
        return _processor.ProcessPendingAsync(batchSize, CancellationToken);
    }

    public void FailPublication(PendingOutboxMessage message, Exception exception)
    {
        EventPublisher
            .Setup(value => value.PublishAsync(
                message.EventType,
                message.Payload,
                CancellationToken))
            .ThrowsAsync(exception);
    }

    public void CancelPublication(PendingOutboxMessage message)
    {
        EventPublisher
            .Setup(value => value.PublishAsync(
                message.EventType,
                message.Payload,
                CancellationToken))
            .Returns(() =>
            {
                _cancellationTokenSource.Cancel();
                return Task.FromCanceled(CancellationToken);
            });
    }

    public void FailMarkProcessed(PendingOutboxMessage message, Exception exception)
    {
        Repository
            .Setup(value => value.MarkProcessedAsync(
                message.Id,
                It.IsAny<DateTime>(),
                CancellationToken))
            .ThrowsAsync(exception);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}
