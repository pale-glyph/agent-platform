using AgentPlatform.Infrastructure;
using AgentPlatform.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Platform")
    ?? throw new InvalidOperationException("ConnectionStrings:Platform must be configured.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddHostedService<ExecutionWorker>();

await builder.Build().RunAsync();
