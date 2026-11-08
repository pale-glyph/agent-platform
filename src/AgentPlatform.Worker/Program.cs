using AgentPlatform.Infrastructure;
using AgentPlatform.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddHostedService<ExecutionWorker>();

await builder.Build().RunAsync();
