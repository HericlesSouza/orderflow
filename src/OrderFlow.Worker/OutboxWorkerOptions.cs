namespace OrderFlow.Worker;

public sealed class OutboxWorkerOptions
{
    public const string SectionName = "OutboxWorker";

    public int BatchSize { get; init; } = 20;
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(5);
}
