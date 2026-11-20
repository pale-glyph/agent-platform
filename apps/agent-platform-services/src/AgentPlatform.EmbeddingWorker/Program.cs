using AgentPlatform.EmbeddingWorker;
using AgentPlatform.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Platform") ?? throw new InvalidOperationException("ConnectionStrings:Platform must be configured.");
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<EmbeddingWorker>();
await builder.Build().RunAsync();
