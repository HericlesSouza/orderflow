using System.Text;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Common.Outbox;
using OrderFlow.Contracts.Events;
using RabbitMQ.Client;

namespace OrderFlow.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(Guid messageId, string eventType, string payload, CancellationToken cancellationToken)
    {
        if (messageId == Guid.Empty)
            throw new ArgumentException("Message ID is required.", nameof(messageId));
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.PublishTimeout);
        try
        {
            await InitializeAsync(timeout.Token);

            var properties = new BasicProperties
            {
                MessageId = messageId.ToString(),
                Type = eventType,
                ContentType = "application/json",
                Persistent = true
            };

            await _channel!.BasicPublishAsync(
                _options.ExchangeName, eventType,
                mandatory: true,
                properties,
                Encoding.UTF8.GetBytes(payload),
                timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await DisposeAsync();
            throw new TimeoutException("RabbitMQ did not confirm publication within the configured timeout.");
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_channel?.IsOpen == true && _connection?.IsOpen == true)
            return;

        await DisposeAsync();
        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            ClientProvidedName = "orderflow-worker",
            RequestedConnectionTimeout = _options.PublishTimeout,
            ContinuationTimeout = _options.PublishTimeout,
            AutomaticRecoveryEnabled = false
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            cancellationToken);

        await _channel.ExchangeDeclareAsync(
            _options.ExchangeName,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-queue-type"] = "quorum" },
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            _options.QueueName,
            _options.ExchangeName,
            EventTypes.OrderDispatched,
            cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var channel = _channel;
        var connection = _connection;
        _channel = null;
        _connection = null;
        try
        {
            if (channel is not null)
                await channel.DisposeAsync();
        }
        finally
        {
            if (connection is not null)
                await connection.DisposeAsync();
        }
    }
}
