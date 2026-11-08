using AgentPlatform.Application.Abstractions;
using AgentPlatform.Application.Runtime;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AgentPlatform.Worker;

public sealed class ExecutionWorker(IServiceScopeFactory scopeFactory, ILogger<ExecutionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Execution worker started.");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        do
        {
            try
            {
                using var discoveryScope = scopeFactory.CreateScope();
                var repository = discoveryScope.ServiceProvider.GetRequiredService<IAgentInstanceRepository>();
                var runnable = await repository.ListRunningAsync(10, stoppingToken);
                foreach (var instance in runnable)
                {
                    using var executionScope = scopeFactory.CreateScope();
                    var runtime = executionScope.ServiceProvider.GetRequiredService<AgentRuntimeService>();
                    try
                    {
                        await runtime.RunAsync(instance.ProjectId, instance.Id, stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Agent instance {InstanceId} failed.", instance.Id);
                        await runtime.MarkFailedAsync(instance.ProjectId, instance.Id, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { logger.LogError(exception, "Could not poll runnable agent instances."); }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
