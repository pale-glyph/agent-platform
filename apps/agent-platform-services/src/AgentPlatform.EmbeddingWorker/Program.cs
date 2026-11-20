using AgentPlatform.EmbeddingWorker;
using AgentPlatform.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Platform") ?? throw new InvalidOperationException("ConnectionStrings:Platform must be configured.");
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<EmbeddingWorker>();
await builder.Build().RunAsync();
