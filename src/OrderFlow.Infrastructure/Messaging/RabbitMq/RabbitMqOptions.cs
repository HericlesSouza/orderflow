namespace OrderFlow.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "orderflow.events";
    public string QueueName { get; set; } = "orderflow.order-dispatched";
    public TimeSpan PublishTimeout { get; set; } = TimeSpan.FromSeconds(15);
}
