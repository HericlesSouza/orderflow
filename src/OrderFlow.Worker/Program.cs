using OrderFlow.Application.Common.Outbox;
using OrderFlow.Infrastructure;
using OrderFlow.Infrastructure.Messaging.RabbitMq;
using OrderFlow.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment.EnvironmentName);

builder.Services.AddScoped<OutboxProcessor>();

builder.Services.AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection("RabbitMq"))
    .Validate(options => !string.IsNullOrWhiteSpace(options.HostName) && options.Port is > 0 and <= 65535,
        "RabbitMq host and port must be valid.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.UserName) && !string.IsNullOrWhiteSpace(options.Password),
        "RabbitMq credentials are required. Configure user secrets or environment variables.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.VirtualHost) &&
        !string.IsNullOrWhiteSpace(options.ExchangeName) && !string.IsNullOrWhiteSpace(options.QueueName),
        "RabbitMq virtual host, exchange and queue names are required.")
    .Validate(options => options.PublishTimeout > TimeSpan.Zero, "RabbitMq publication timeout must be positive.")
    .ValidateOnStart();

builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

builder.Services
    .AddOptions<OutboxWorkerOptions>()
    .Bind(builder.Configuration.GetSection(OutboxWorkerOptions.SectionName))
    .Validate(options => options.BatchSize > 0, "OutboxWorker:BatchSize must be greater than zero.")
    .Validate(options => options.PollingInterval > TimeSpan.Zero, "OutboxWorker:PollingInterval must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
