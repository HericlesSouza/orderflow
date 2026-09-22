using System.Text.Json.Nodes;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Outbox;

[Collection<OutboxCollection>]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Outbox")]
public sealed class OutboxMessageRepositoryTests(
    OrderFlowIntegrationSuiteFixture environment)
    : FeatureApiTest(environment, IntegrationFeatures.Outbox)
{
    private static readonly DateTime BaseCreatedAt = new(
        2026,
        9,
        21,
        12,
        0,
        0,
        DateTimeKind.Utc);

    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Should_Return_Only_Pending_Messages()
    {
        await using var fixture = new OutboxMessageRepositoryFixture(Services);
        var pendingMessage = CreateMessage(
            eventType: "order.pending.v1",
            payload: "{\"status\":\"pending\"}");
        var processedMessage = CreateMessage(
            eventType: "order.processed.v1",
            payload: "{\"status\":\"processed\"}",
            processedAt: BaseCreatedAt.AddMinutes(1));
        await fixture.InsertAsync(pendingMessage, CancellationToken);
        await fixture.InsertAsync(processedMessage, CancellationToken);

        var messages = await fixture.Repository.GetPendingAsync(20, CancellationToken);

        var actual = Assert.Single(messages);
        Assert.Equal(pendingMessage.Id, actual.Id);
        Assert.Equal(pendingMessage.EventType, actual.EventType);
        Assert.True(JsonNode.DeepEquals(
            JsonNode.Parse(pendingMessage.Payload),
            JsonNode.Parse(actual.Payload)));
    }

    [Fact]
    public async Task Should_Return_Pending_Messages_In_Creation_Order()
    {
        await using var fixture = new OutboxMessageRepositoryFixture(Services);
        var firstMessage = CreateMessage(createdAt: BaseCreatedAt);
        var secondMessage = CreateMessage(createdAt: BaseCreatedAt.AddMinutes(1));
        var thirdMessage = CreateMessage(createdAt: BaseCreatedAt.AddMinutes(2));
        await fixture.InsertAsync(thirdMessage, CancellationToken);
        await fixture.InsertAsync(firstMessage, CancellationToken);
        await fixture.InsertAsync(secondMessage, CancellationToken);

        var messages = await fixture.Repository.GetPendingAsync(20, CancellationToken);

        Assert.Equal(
            [firstMessage.Id, secondMessage.Id, thirdMessage.Id],
            messages.Select(message => message.Id));
    }

    [Fact]
    public async Task Should_Respect_Batch_Size_When_Getting_Pending_Messages()
    {
        await using var fixture = new OutboxMessageRepositoryFixture(Services);
        var firstMessage = CreateMessage(createdAt: BaseCreatedAt);
        var secondMessage = CreateMessage(createdAt: BaseCreatedAt.AddMinutes(1));
        var thirdMessage = CreateMessage(createdAt: BaseCreatedAt.AddMinutes(2));
        await fixture.InsertAsync(firstMessage, CancellationToken);
        await fixture.InsertAsync(secondMessage, CancellationToken);
        await fixture.InsertAsync(thirdMessage, CancellationToken);

        var messages = await fixture.Repository.GetPendingAsync(2, CancellationToken);

        Assert.Equal(
            [firstMessage.Id, secondMessage.Id],
            messages.Select(message => message.Id));
    }

    [Fact]
    public async Task Should_Mark_Message_As_Processed()
    {
        await using var fixture = new OutboxMessageRepositoryFixture(Services);
        var previousAttemptAt = BaseCreatedAt.AddMinutes(1);
        var attemptedAt = BaseCreatedAt.AddMinutes(2);
        var message = CreateMessage(
            attemptCount: 1,
            lastAttemptedAt: previousAttemptAt,
            lastError: "Previous failure.");
        await fixture.InsertAsync(message, CancellationToken);

        await fixture.Repository.MarkProcessedAsync(message.Id, attemptedAt, CancellationToken);

        var persistedMessage = await fixture.GetByIdAsync(message.Id, CancellationToken);
        Assert.Equal(attemptedAt, persistedMessage.ProcessedAt);
        Assert.Equal(attemptedAt, persistedMessage.LastAttemptedAt);
        Assert.Equal(2, persistedMessage.AttemptCount);
        Assert.Null(persistedMessage.LastError);
    }

    [Fact]
    public async Task Should_Mark_Message_As_Failed()
    {
        await using var fixture = new OutboxMessageRepositoryFixture(Services);
        var attemptedAt = BaseCreatedAt.AddMinutes(1);
        var message = CreateMessage();
        await fixture.InsertAsync(message, CancellationToken);

        await fixture.Repository.MarkFailedAsync(
            message.Id,
            attemptedAt,
            "Publisher unavailable.",
            CancellationToken);

        var persistedMessage = await fixture.GetByIdAsync(message.Id, CancellationToken);
        Assert.Null(persistedMessage.ProcessedAt);
        Assert.Equal(attemptedAt, persistedMessage.LastAttemptedAt);
        Assert.Equal(1, persistedMessage.AttemptCount);
        Assert.Equal("Publisher unavailable.", persistedMessage.LastError);
    }

    [Fact]
    public async Task Should_Accumulate_Attempts_When_Message_Fails_More_Than_Once()
    {
        await using var fixture = new OutboxMessageRepositoryFixture(Services);
        var firstAttemptAt = BaseCreatedAt.AddMinutes(1);
        var secondAttemptAt = BaseCreatedAt.AddMinutes(2);
        var message = CreateMessage();
        await fixture.InsertAsync(message, CancellationToken);

        await fixture.Repository.MarkFailedAsync(
            message.Id,
            firstAttemptAt,
            "First failure.",
            CancellationToken);
        await fixture.Repository.MarkFailedAsync(
            message.Id,
            secondAttemptAt,
            "Second failure.",
            CancellationToken);

        var persistedMessage = await fixture.GetByIdAsync(message.Id, CancellationToken);
        Assert.Null(persistedMessage.ProcessedAt);
        Assert.Equal(secondAttemptAt, persistedMessage.LastAttemptedAt);
        Assert.Equal(2, persistedMessage.AttemptCount);
        Assert.Equal("Second failure.", persistedMessage.LastError);
    }

    [Fact]
    public async Task Should_Not_Change_Already_Processed_Message()
    {
        await using var fixture = new OutboxMessageRepositoryFixture(Services);
        var processedAt = BaseCreatedAt.AddMinutes(1);
        var message = CreateMessage(
            processedAt: processedAt,
            attemptCount: 1,
            lastAttemptedAt: processedAt);
        await fixture.InsertAsync(message, CancellationToken);

        await fixture.Repository.MarkFailedAsync(
            message.Id,
            BaseCreatedAt.AddMinutes(2),
            "Late failure.",
            CancellationToken);
        await fixture.Repository.MarkProcessedAsync(
            message.Id,
            BaseCreatedAt.AddMinutes(3),
            CancellationToken);

        var persistedMessage = await fixture.GetByIdAsync(message.Id, CancellationToken);
        Assert.Equal(processedAt, persistedMessage.ProcessedAt);
        Assert.Equal(processedAt, persistedMessage.LastAttemptedAt);
        Assert.Equal(1, persistedMessage.AttemptCount);
        Assert.Null(persistedMessage.LastError);
    }

    private static OutboxMessageRow CreateMessage(
        string eventType = "order.dispatched.v1",
        string payload = "{\"orderId\":\"5fd3bc50-07e9-4c76-a858-0dc796b68ab1\"}",
        DateTime? createdAt = null,
        DateTime? processedAt = null,
        int attemptCount = 0,
        DateTime? lastAttemptedAt = null,
        string? lastError = null)
    {
        return new OutboxMessageRow(
            Guid.NewGuid(),
            eventType,
            payload,
            createdAt ?? BaseCreatedAt,
            processedAt,
            attemptCount,
            lastAttemptedAt,
            lastError);
    }
}
