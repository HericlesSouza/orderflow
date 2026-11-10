using System.Text;
using Microsoft.Extensions.Options;
using OrderFlow.Application.Common.Outbox;
using OrderFlow.Contracts.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderFlow.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqEventPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private IConnection? _connection;
    private IChannel? _channel;
    // Recovery callbacks run in the client background task, while the Worker publishes sequentially.
    private volatile bool _isRecovering;

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
            throw new TimeoutException("RabbitMQ initialization or publication exceeded the configured timeout.");
        }
    }

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        // Let the client finish recovering the connection, channels and topology.
        if (_isRecovering || _connection is { IsOpen: false })
            throw new InvalidOperationException("RabbitMQ connection is recovering. The Outbox will retry publication.");

        if (_channel?.IsOpen == true && _connection?.IsOpen == true)
            return;

        // A channel closed by a protocol error is not recovered automatically.
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
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        try
        {
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
            _connection.RecoverySucceededAsync += OnRecoverySucceededAsync;
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
        catch
        {
            // An incomplete initialization must start again on the next Outbox attempt.
            await DisposeAsync();
            throw;
        }
    }

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs args)
    {
        _isRecovering = true;
        return Task.CompletedTask;
    }

    private Task OnRecoverySucceededAsync(object sender, AsyncEventArgs args)
    {
        _isRecovering = false;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        var channel = _channel;
        var connection = _connection;
        _channel = null;
        _connection = null;
        if (connection is not null)
        {
            connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
            connection.RecoverySucceededAsync -= OnRecoverySucceededAsync;
        }
        try
        {
            if (channel is not null)
                await channel.DisposeAsync();
        }
        finally
        {
            if (connection is not null)
                await connection.DisposeAsync();
            _isRecovering = false;
        }
    }
}
