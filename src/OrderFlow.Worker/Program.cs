using OrderFlow.Application.Common.Outbox;
using OrderFlow.Infrastructure;
using OrderFlow.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment.EnvironmentName);

builder.Services.AddScoped<OutboxProcessor>();

builder.Services.AddSingleton<IEventPublisher, LoggingEventPublisher>();

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
