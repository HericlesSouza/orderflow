using System.Text.Json.Nodes;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Common.Results;
using OrderFlow.Contracts.Events;
using OrderFlow.Contracts.Events.Orders;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Outbox;

[Collection<OutboxCollection>]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Outbox")]
public sealed class OutboxRepositoryTests(
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
    public async Task Should_Commit_Different_Event_Types_Only_When_Unit_Of_Work_Succeeds()
    {
        await using var fixture = new OutboxRepositoryFixture(Services);
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var dispatchedAt = BaseCreatedAt.AddMinutes(1);
        var message = new OrderDispatched(orderId, dispatchedAt, [new OrderDispatchedItem(productId, 2)]);

        var result = await fixture.UnitOfWork.ExecuteInTransactionAsync(async cancellationToken =>
        {
            fixture.Repository.Add(EventTypes.OrderDispatched, message);
            fixture.Repository.Add("inventory.adjusted.v1", new { ProductId = productId, Quantity = 3 });

            Assert.Empty(await fixture.Repository.GetPendingAsync(20, cancellationToken));

            return Result<bool>.Success(true);
        }, CancellationToken);

        Assert.True(result.IsSuccess);
        var messages = await fixture.Repository.GetPendingAsync(20, CancellationToken);
        Assert.Equal(2, messages.Count);

        var dispatched = Assert.Single(messages, value => value.EventType == "order.dispatched.v1");
        var dispatchedPayload = JsonNode.Parse(dispatched.Payload)!;
        Assert.Equal(orderId, dispatchedPayload["orderId"]!.GetValue<Guid>());
        Assert.Equal(dispatchedAt, dispatchedPayload["dispatchedAtUtc"]!.GetValue<DateTime>());
        var item = Assert.Single(dispatchedPayload["items"]!.AsArray())!;
        Assert.Equal(productId, item["productId"]!.GetValue<Guid>());
        Assert.Equal(2, item["quantity"]!.GetValue<int>());

        var adjusted = Assert.Single(messages, value => value.EventType == "inventory.adjusted.v1");
        var adjustedPayload = JsonNode.Parse(adjusted.Payload)!;
        Assert.Equal(productId, adjustedPayload["productId"]!.GetValue<Guid>());
        Assert.Equal(3, adjustedPayload["quantity"]!.GetValue<int>());

        foreach (var pending in messages)
        {
            var persisted = await fixture.GetByIdAsync(pending.Id, CancellationToken);
            Assert.NotEqual(Guid.Empty, persisted.Id);
            Assert.NotEqual(default, persisted.CreatedAt);
            Assert.Equal(DateTimeKind.Utc, persisted.CreatedAt.Kind);
            Assert.Equal(0, persisted.AttemptCount);
            Assert.Null(persisted.ProcessedAt);
            Assert.Null(persisted.LastAttemptedAt);
            Assert.Null(persisted.LastError);
        }
    }

    [Fact]
    public async Task Should_Discard_Message_When_Unit_Of_Work_Fails()
    {
        await using var fixture = new OutboxRepositoryFixture(Services);
        var message = new OrderDispatched(Guid.NewGuid(), BaseCreatedAt, []);

        var result = await fixture.UnitOfWork.ExecuteInTransactionAsync(_ =>
        {
            fixture.Repository.Add(EventTypes.OrderDispatched, message);
            return Task.FromResult(Result<bool>.Failure(AppError.Conflict("test.failure", "Simulated failure.")));
        }, CancellationToken);

        Assert.True(result.IsFailure);
        Assert.Empty(await fixture.Repository.GetPendingAsync(20, CancellationToken));

        await fixture.UnitOfWork.ExecuteInTransactionAsync(
            _ => Task.FromResult(Result<bool>.Success(true)), CancellationToken);

        Assert.Empty(await fixture.Repository.GetPendingAsync(20, CancellationToken));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Should_Reject_Empty_Event_Type(string? eventType)
    {
        await using var fixture = new OutboxRepositoryFixture(Services);

        Assert.ThrowsAny<ArgumentException>(() => fixture.Repository.Add(eventType!, new { OrderId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task Should_Reject_Null_Message()
    {
        await using var fixture = new OutboxRepositoryFixture(Services);

        Assert.Throws<ArgumentNullException>(() => fixture.Repository.Add<OrderDispatched>(EventTypes.OrderDispatched, null!));
    }

    [Fact]
    public async Task Should_Return_Only_Pending_Messages()
    {
        await using var fixture = new OutboxRepositoryFixture(Services);
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
        await using var fixture = new OutboxRepositoryFixture(Services);
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
        await using var fixture = new OutboxRepositoryFixture(Services);
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
        await using var fixture = new OutboxRepositoryFixture(Services);
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
        await using var fixture = new OutboxRepositoryFixture(Services);
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
        await using var fixture = new OutboxRepositoryFixture(Services);
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
        await using var fixture = new OutboxRepositoryFixture(Services);
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
