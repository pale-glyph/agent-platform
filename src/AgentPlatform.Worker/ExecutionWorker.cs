namespace AgentPlatform.Worker;

public sealed class ExecutionWorker(ILogger<ExecutionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Execution worker started.");

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }
}
