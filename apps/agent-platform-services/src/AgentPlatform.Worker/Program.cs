using AgentPlatform.Infrastructure;
using AgentPlatform.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddHostedService<ExecutionWorker>();

await builder.Build().RunAsync();
