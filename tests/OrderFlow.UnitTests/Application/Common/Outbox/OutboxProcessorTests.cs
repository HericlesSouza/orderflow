using Moq;
using OrderFlow.Application.Common.Outbox;

namespace OrderFlow.UnitTests.Application.Common.Outbox;

public sealed class OutboxProcessorTests
{
    [Fact]
    public async Task Should_Mark_Message_As_Processed_When_Publication_Succeeds()
    {
        var message = CreateMessage();
        using var fixture = new OutboxProcessorFixture(message);

        await fixture.ProcessPendingAsync();

        fixture.EventPublisher.Verify(
            value => value.PublishAsync(message.EventType, message.Payload, fixture.CancellationToken),
            Times.Once);
        fixture.Repository.Verify(
            value => value.MarkProcessedAsync(
                message.Id,
                It.Is<DateTime>(attemptedAt => IsValidAttemptedAt(attemptedAt)),
                fixture.CancellationToken),
            Times.Once);
        fixture.Repository.Verify(
            value => value.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Mark_Message_As_Failed_When_Publication_Fails()
    {
        var message = CreateMessage();
        var exception = new InvalidOperationException("Publisher unavailable.");
        using var fixture = new OutboxProcessorFixture(message);
        fixture.FailPublication(message, exception);

        await fixture.ProcessPendingAsync();

        fixture.Repository.Verify(
            value => value.MarkFailedAsync(
                message.Id,
                It.Is<DateTime>(attemptedAt => IsValidAttemptedAt(attemptedAt)),
                "InvalidOperationException: Publisher unavailable.",
                fixture.CancellationToken),
            Times.Once);
        fixture.Repository.Verify(
            value => value.MarkProcessedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Do_Nothing_When_No_Pending_Messages_Are_Found()
    {
        using var fixture = new OutboxProcessorFixture();

        await fixture.ProcessPendingAsync();

        fixture.Repository.Verify(
            value => value.GetPendingAsync(OutboxProcessorFixture.BatchSize, fixture.CancellationToken),
            Times.Once);
        fixture.EventPublisher.Verify(
            value => value.PublishAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Repository.Verify(
            value => value.MarkProcessedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Repository.Verify(
            value => value.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Use_Configured_Batch_Size()
    {
        const int configuredBatchSize = 50;
        using var fixture = new OutboxProcessorFixture();

        await fixture.ProcessPendingAsync(configuredBatchSize);

        fixture.Repository.Verify(
            value => value.GetPendingAsync(configuredBatchSize, fixture.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Should_Continue_Processing_When_A_Message_Publication_Fails()
    {
        var failedMessage = CreateMessage("order.failed.v1");
        var successfulMessage = CreateMessage("order.succeeded.v1");
        using var fixture = new OutboxProcessorFixture(failedMessage, successfulMessage);
        fixture.FailPublication(
            failedMessage,
            new InvalidOperationException("First publication failed."));

        await fixture.ProcessPendingAsync();

        fixture.EventPublisher.Verify(
            value => value.PublishAsync(
                failedMessage.EventType,
                failedMessage.Payload,
                fixture.CancellationToken),
            Times.Once);
        fixture.EventPublisher.Verify(
            value => value.PublishAsync(
                successfulMessage.EventType,
                successfulMessage.Payload,
                fixture.CancellationToken),
            Times.Once);
        fixture.Repository.Verify(
            value => value.MarkFailedAsync(
                failedMessage.Id,
                It.Is<DateTime>(attemptedAt => IsValidAttemptedAt(attemptedAt)),
                "InvalidOperationException: First publication failed.",
                fixture.CancellationToken),
            Times.Once);
        fixture.Repository.Verify(
            value => value.MarkProcessedAsync(
                successfulMessage.Id,
                It.Is<DateTime>(attemptedAt => IsValidAttemptedAt(attemptedAt)),
                fixture.CancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Should_Propagate_Cancellation_Without_Marking_Message()
    {
        var message = CreateMessage();
        using var fixture = new OutboxProcessorFixture(message);
        fixture.CancelPublication(message);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => fixture.ProcessPendingAsync());

        fixture.Repository.Verify(
            value => value.MarkProcessedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        fixture.Repository.Verify(
            value => value.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Propagate_Exception_When_Marking_Message_As_Processed_Fails()
    {
        var message = CreateMessage();
        var exception = new InvalidOperationException("Could not mark message as processed.");
        using var fixture = new OutboxProcessorFixture(message);
        fixture.FailMarkProcessed(message, exception);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.ProcessPendingAsync());

        Assert.Same(exception, actual);
        fixture.EventPublisher.Verify(
            value => value.PublishAsync(message.EventType, message.Payload, fixture.CancellationToken),
            Times.Once);
        fixture.Repository.Verify(
            value => value.MarkFailedAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Truncate_Error_When_Error_Message_Exceeds_Maximum_Length()
    {
        var message = CreateMessage();
        var exception = new InvalidOperationException(new string('x', 2100));
        using var fixture = new OutboxProcessorFixture(message);
        fixture.FailPublication(message, exception);

        await fixture.ProcessPendingAsync();

        fixture.Repository.Verify(
            value => value.MarkFailedAsync(
                message.Id,
                It.Is<DateTime>(attemptedAt => IsValidAttemptedAt(attemptedAt)),
                It.Is<string>(error =>
                    error.Length == 2000 &&
                    error.StartsWith("InvalidOperationException: ", StringComparison.Ordinal)),
                fixture.CancellationToken),
            Times.Once);
    }

    private static bool IsValidAttemptedAt(DateTime attemptedAt)
    {
        return attemptedAt != default && attemptedAt.Kind == DateTimeKind.Utc;
    }

    private static PendingOutboxMessage CreateMessage(string eventType = "order.dispatched.v1")
    {
        return new PendingOutboxMessage(
            Guid.NewGuid(),
            eventType,
            "{\"orderId\":\"5fd3bc50-07e9-4c76-a858-0dc796b68ab1\"}");
    }

}
