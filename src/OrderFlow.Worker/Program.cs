using OrderFlow.Application.Common.Outbox;
using OrderFlow.Infrastructure;
using OrderFlow.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment.EnvironmentName);

builder.Services.AddScoped<OutboxProcessor>();

builder.Services.AddSingleton<IEventPublisher, LoggingEventPublisher>();

builder.Services
    .AddOptions<OutboxWorkerOptions>()
    .Bind(builder.Configuration.GetSection(OutboxWorkerOptions.SectionName))
    .Validate(options => options.BatchSize > 0, "OutboxWorker:BatchSize must be greater than zero.")
    .Validate(options => options.PollingInterval > TimeSpan.Zero, "OutboxWorker:PollingInterval must be greater than zero.")
    .ValidateOnStart();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
