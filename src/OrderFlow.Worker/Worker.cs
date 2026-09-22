using Microsoft.Extensions.Options;
using OrderFlow.Application.Common.Outbox;

namespace OrderFlow.Worker;

public class Worker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxWorkerOptions> options,
    ILogger<Worker> logger) : BackgroundService
{
    private readonly OutboxWorkerOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

                await processor.ProcessPendingAsync(_options.BatchSize, stoppingToken);

                await Task.Delay(
                    _options.PollingInterval,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Outbox processing cycle failed.");

                await Task.Delay(
                    _options.PollingInterval,
                    stoppingToken);
            }
        }
    }
}
