using KnowledgeBase.Service;
using KnowledgeBase.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("KnowledgeBase") ?? throw new InvalidOperationException("ConnectionStrings:KnowledgeBase must be configured.");
builder.Services.AddKnowledgeBasePersistence(connectionString);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHostedService<EmbeddingWorker>();
await builder.Build().RunAsync();
