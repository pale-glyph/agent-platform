using AgentPlatform.Infrastructure;
using AgentPlatform.Application.Runtime;
using AgentPlatform.Application.Mcp;
using AgentPlatform.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Platform")
    ?? throw new InvalidOperationException("ConnectionStrings:Platform must be configured.");

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<AgentRuntimeService>();
builder.Services.AddScoped<McpServerService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<ExecutionWorker>();

await builder.Build().RunAsync();
