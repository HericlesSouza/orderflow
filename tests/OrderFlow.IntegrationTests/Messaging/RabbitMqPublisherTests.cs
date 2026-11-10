using System.Text;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Common.Outbox;
using OrderFlow.Contracts.Events;
using OrderFlow.Infrastructure.Messaging.RabbitMq;
using OrderFlow.IntegrationTests.Infrastructure;
using OrderFlow.IntegrationTests.Outbox;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace OrderFlow.IntegrationTests.Messaging;

[CollectionDefinition]
public sealed class RabbitMqPublishingCollection : ICollectionFixture<RabbitMqBrokerFixture>;

public sealed class RabbitMqBrokerFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder("rabbitmq:4.4.0-management")
        .WithPortBinding(5672, true)
        .WithEnvironment("RABBITMQ_DEFAULT_USER", "publisher_test")
        .WithEnvironment("RABBITMQ_DEFAULT_PASS", "publisher_test_only")
        .WithEnvironment("RABBITMQ_SERVER_ADDITIONAL_ERL_ARGS", "+S 2:2")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(5672))
        .Build();

    public string HostName => _container.Hostname;
    public int Port => _container.GetMappedPublicPort(5672);
    public async ValueTask InitializeAsync() => await _container.StartAsync();
    public ValueTask DisposeAsync() => _container.DisposeAsync();
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var result = await _container.ExecAsync(["rabbitmqctl", "stop_app"], cancellationToken);
        Assert.Equal(0, result.ExitCode);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await _container.ExecAsync(["rabbitmqctl", "start_app"], cancellationToken);
        Assert.Equal(0, result.ExitCode);
    }

    public async Task WaitForPublisherConnectionAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        while (true)
        {
            var result = await _container.ExecAsync(
                ["rabbitmqctl", "list_connections", "--silent", "--formatter", "json", "client_properties"],
                timeout.Token);
            if (result.ExitCode == 0 && result.Stdout.Contains("orderflow-worker", StringComparison.Ordinal))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(500), timeout.Token);
        }
    }
}

[Collection<RabbitMqPublishingCollection>]
[Trait("Layer", "Infrastructure")]
[Trait("Feature", "Messaging")]
public sealed class RabbitMqPublisherTests(OrderFlowIntegrationSuiteFixture environment, RabbitMqBrokerFixture broker)
    : FeatureApiTest(environment, "Publishing")
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Should_Confirm_Publication_Before_Completing_Outbox_And_Preserve_Message_Identity()
    {
        var options = CreateOptions();
        await using var publisher = CreatePublisher(options);
        await using var fixture = new OutboxRepositoryFixture(Services);
        var message = new OutboxMessageRow(Guid.NewGuid(), EventTypes.OrderDispatched,
            "{\"orderId\":\"5fd3bc50-07e9-4c76-a858-0dc796b68ab1\"}", DateTime.UtcNow);
        await fixture.InsertAsync(message, CancellationToken);

        var processor = new OutboxProcessor(fixture.Repository, publisher, NullLogger<OutboxProcessor>.Instance);
        await processor.ProcessPendingAsync(20, CancellationToken);

        var persisted = await fixture.GetByIdAsync(message.Id, CancellationToken);
        Assert.NotNull(persisted.ProcessedAt);
        Assert.Equal(1, persisted.AttemptCount);
        Assert.Null(persisted.LastError);

        await using var connection = await ConnectAsync(options);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);
        var received = await channel.BasicGetAsync(options.QueueName, autoAck: false, CancellationToken);
        Assert.NotNull(received);
        Assert.Equal(message.Id.ToString(), received.BasicProperties.MessageId);
        Assert.Equal(message.EventType, received.BasicProperties.Type);
        Assert.True(received.BasicProperties.Persistent);
        Assert.Equal("application/json", received.BasicProperties.ContentType);
        Assert.Equal(persisted.Payload, Encoding.UTF8.GetString(received.Body.Span));
        await channel.BasicAckAsync(received.DeliveryTag, false, CancellationToken);

        // A second publication uses the same initialized publisher and route.
        await publisher.PublishAsync(message.Id, message.EventType, message.Payload, CancellationToken);
        var repeated = await channel.BasicGetAsync(options.QueueName, autoAck: true, CancellationToken);
        Assert.NotNull(repeated);
        Assert.Equal(message.Id.ToString(), repeated.BasicProperties.MessageId);
    }

    [Fact]
    public async Task Should_Keep_Outbox_Pending_When_Mandatory_Publication_Has_No_Route()
    {
        var options = CreateOptions();
        await using var publisher = CreatePublisher(options);
        await using var fixture = new OutboxRepositoryFixture(Services);
        var message = new OutboxMessageRow(Guid.NewGuid(), "unknown.event.v1", "{}", DateTime.UtcNow);
        await fixture.InsertAsync(message, CancellationToken);

        var processor = new OutboxProcessor(fixture.Repository, publisher, NullLogger<OutboxProcessor>.Instance);
        await processor.ProcessPendingAsync(20, CancellationToken);

        var persisted = await fixture.GetByIdAsync(message.Id, CancellationToken);
        Assert.Null(persisted.ProcessedAt);
        Assert.Equal(1, persisted.AttemptCount);
        Assert.Contains("NO_ROUTE", persisted.LastError);
        Assert.Single(await fixture.Repository.GetPendingAsync(20, CancellationToken));

        var returned = await Assert.ThrowsAnyAsync<PublishException>(() => publisher.PublishAsync(
            message.Id, message.EventType, message.Payload, CancellationToken));
        Assert.True(returned.IsReturn);
    }

    [Fact]
    public async Task Should_Retry_Initial_Connection_And_Publish_Pending_Outbox_With_The_Same_Id()
    {
        var options = CreateOptions();
        options.PublishTimeout = TimeSpan.FromSeconds(2);
        await using var publisher = CreatePublisher(options);
        await using var fixture = new OutboxRepositoryFixture(Services);
        var message = new OutboxMessageRow(Guid.NewGuid(), EventTypes.OrderDispatched, "{}", DateTime.UtcNow);
        await fixture.InsertAsync(message, CancellationToken);
        var processor = new OutboxProcessor(fixture.Repository, publisher, NullLogger<OutboxProcessor>.Instance);

        await broker.StopAsync(CancellationToken);
        try
        {
            await processor.ProcessPendingAsync(20, CancellationToken);
            var pending = await fixture.GetByIdAsync(message.Id, CancellationToken);
            Assert.Null(pending.ProcessedAt);
            Assert.Equal(1, pending.AttemptCount);
            Assert.NotNull(pending.LastError);
        }
        finally
        {
            await broker.StartAsync(CancellationToken);
        }

        await ProcessUntilCompletedAsync(processor, fixture, message.Id);
        await AssertPublishedMessageAsync(options, message.Id);
    }

    [Fact]
    public async Task Should_Recover_Connection_Automatically_And_Then_Retry_Pending_Outbox()
    {
        var options = CreateOptions();
        await using var publisher = CreatePublisher(options);
        var firstMessageId = Guid.NewGuid();
        await publisher.PublishAsync(firstMessageId, EventTypes.OrderDispatched, "{}", CancellationToken);
        await AssertPublishedMessageAsync(options, firstMessageId);

        await using var fixture = new OutboxRepositoryFixture(Services);
        var message = new OutboxMessageRow(Guid.NewGuid(), EventTypes.OrderDispatched, "{}", DateTime.UtcNow);
        await fixture.InsertAsync(message, CancellationToken);
        var processor = new OutboxProcessor(fixture.Repository, publisher, NullLogger<OutboxProcessor>.Instance);

        await broker.StopAsync(CancellationToken);
        try
        {
            await processor.ProcessPendingAsync(20, CancellationToken);
            await processor.ProcessPendingAsync(20, CancellationToken);
            var pending = await fixture.GetByIdAsync(message.Id, CancellationToken);
            Assert.Null(pending.ProcessedAt);
            Assert.Equal(2, pending.AttemptCount);
            Assert.NotNull(pending.LastError);
        }
        finally
        {
            await broker.StartAsync(CancellationToken);
        }

        // No PublishAsync call here: the library must reconnect on its own.
        await broker.WaitForPublisherConnectionAsync(CancellationToken);
        await ProcessUntilCompletedAsync(processor, fixture, message.Id);
        await AssertPublishedMessageAsync(options, message.Id);
    }

    [Fact]
    public async Task Should_Recreate_Channel_After_A_Protocol_Error_And_Restore_Routing()
    {
        var options = CreateOptions();
        await using var publisher = CreatePublisher(options);
        var firstMessageId = Guid.NewGuid();
        await publisher.PublishAsync(firstMessageId, EventTypes.OrderDispatched, "{}", CancellationToken);
        await AssertPublishedMessageAsync(options, firstMessageId);

        await using var connection = await ConnectAsync(options);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);
        await channel.ExchangeDeleteAsync(options.ExchangeName, cancellationToken: CancellationToken);

        var messageId = Guid.NewGuid();
        await Assert.ThrowsAsync<AlreadyClosedException>(() => publisher.PublishAsync(
            messageId, EventTypes.OrderDispatched, "{}", CancellationToken));

        await publisher.PublishAsync(messageId, EventTypes.OrderDispatched, "{}", CancellationToken);
        await AssertPublishedMessageAsync(options, messageId);
    }

    private async Task ProcessUntilCompletedAsync(OutboxProcessor processor, OutboxRepositoryFixture fixture, Guid messageId)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));
        while (true)
        {
            await processor.ProcessPendingAsync(20, timeout.Token);
            var persisted = await fixture.GetByIdAsync(messageId, timeout.Token);
            if (persisted.ProcessedAt is not null)
            {
                Assert.Null(persisted.LastError);
                Assert.True(persisted.AttemptCount >= 2);
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200), timeout.Token);
        }
    }

    private async Task AssertPublishedMessageAsync(RabbitMqOptions options, Guid messageId)
    {
        await using var connection = await ConnectAsync(options);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken);
        var received = await channel.BasicGetAsync(options.QueueName, autoAck: true, CancellationToken);
        Assert.NotNull(received);
        Assert.Equal(messageId.ToString(), received.BasicProperties.MessageId);
        Assert.Equal(EventTypes.OrderDispatched, received.BasicProperties.Type);
    }

    private RabbitMqOptions CreateOptions() => new()
    {
        HostName = broker.HostName,
        Port = broker.Port,
        UserName = "publisher_test",
        Password = "publisher_test_only",
        ExchangeName = $"orderflow.tests.{Guid.NewGuid():N}",
        QueueName = $"orderflow.tests.{Guid.NewGuid():N}"
    };

    private static RabbitMqEventPublisher CreatePublisher(RabbitMqOptions options) =>
        new(Options.Create(options));

    private Task<IConnection> ConnectAsync(RabbitMqOptions options) => new ConnectionFactory
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password
    }.CreateConnectionAsync(CancellationToken);
}
