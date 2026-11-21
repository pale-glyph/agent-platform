using System.Text.Json;
using KnowledgeBase.Service;
using KnowledgeBase.Service.Persistence;
using KnowledgeBase.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("KnowledgeBase") ?? throw new InvalidOperationException("ConnectionStrings:KnowledgeBase must be configured.");
var clerkIssuer = builder.Configuration["Clerk:Issuer"] ?? throw new InvalidOperationException("Clerk:Issuer must be configured.");
builder.Services.AddKnowledgeBasePersistence(connectionString);
builder.Services.AddScoped<KnowledgeBaseService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => { options.Authority = clerkIssuer; options.TokenValidationParameters.ValidateAudience = false; });
var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope()) { await scope.ServiceProvider.GetRequiredService<KnowledgeBaseDbContext>().Database.MigrateAsync(); }
app.MapHealthChecks("/health"); app.UseAuthentication(); app.UseAuthorization(); app.MapOpenApi();

app.MapPost("/internal/projects/{projectId:guid}/knowledge-bases/validate", async (Guid projectId, KnowledgeBaseValidationRequest request, HttpRequest httpRequest, KnowledgeBaseDbContext db, IConfiguration configuration, CancellationToken ct) =>
{
    var expectedKey = configuration["KnowledgeBase:ServiceKey"];
    if (string.IsNullOrWhiteSpace(expectedKey) || !string.Equals(httpRequest.Headers["X-Knowledge-Base-Service-Key"], expectedKey, StringComparison.Ordinal)) return Results.Unauthorized();
    var ids = request.KnowledgeBaseIds ?? [];
    var valid = ids.Count == ids.Distinct().Count() && await db.KnowledgeBases.CountAsync(item => item.ProjectId == projectId && item.DeletedAt == null && ids.Contains(item.Id), ct) == ids.Count;
    return Results.Ok(new { valid });
});

var bases = app.MapGroup("/api/projects/{projectId:guid}/knowledge-bases").RequireAuthorization();
bases.MapGet("/", async (Guid projectId, int? limit, KnowledgeBaseService service, CancellationToken ct) => Results.Ok(await service.ListAsync(projectId, Math.Clamp(limit ?? 25, 1, 100), ct)));
bases.MapPost("/", async (Guid projectId, KnowledgeBaseRequest request, KnowledgeBaseService service, CancellationToken ct) => { try { var item=await service.CreateAsync(projectId,request.Name??string.Empty,request.Description,ct); return item is null?Results.NotFound():Results.Created($"/api/projects/{projectId}/knowledge-bases/{item.Id}",item); } catch(ArgumentException e) { return Results.ValidationProblem(new Dictionary<string,string[]> { ["knowledgeBase"]=[e.Message] }); }});
bases.MapGet("/{knowledgeBaseId:guid}", async (Guid projectId, Guid knowledgeBaseId, KnowledgeBaseService service, CancellationToken ct) => (await service.FindAsync(projectId,knowledgeBaseId,ct)) is { } item ? Results.Ok(item) : Results.NotFound());
bases.MapPatch("/{knowledgeBaseId:guid}", async (Guid projectId, Guid knowledgeBaseId, KnowledgeBaseRequest request, KnowledgeBaseService service, CancellationToken ct) => { try { return (await service.UpdateAsync(projectId,knowledgeBaseId,request.Name??string.Empty,request.Description,ct)) is { } item?Results.Ok(item):Results.NotFound(); } catch(ArgumentException e) { return Results.ValidationProblem(new Dictionary<string,string[]> { ["knowledgeBase"]=[e.Message] }); }});
bases.MapDelete("/{knowledgeBaseId:guid}", async(Guid projectId,Guid knowledgeBaseId,KnowledgeBaseService service,CancellationToken ct) => await service.DeleteAsync(projectId,knowledgeBaseId,ct)?Results.Accepted():Results.NotFound());
bases.MapGet("/{knowledgeBaseId:guid}/documents",async(Guid projectId,Guid knowledgeBaseId,KnowledgeBaseService service,CancellationToken ct)=>Results.Ok(await service.ListDocumentsAsync(projectId,knowledgeBaseId,ct)));
bases.MapPost("/{knowledgeBaseId:guid}/documents",async(Guid projectId,Guid knowledgeBaseId,DocumentCreateRequest request,KnowledgeBaseService service,CancellationToken ct)=> { try {var item=await service.CreateDocumentAsync(projectId,knowledgeBaseId,request.Title??string.Empty,request.ExternalId,request.Content??string.Empty,request.Metadata,ct);return item is null?Results.NotFound():Results.Accepted($"/api/projects/{projectId}/knowledge-bases/{knowledgeBaseId}/documents/{item.Value.Document.Id}",new {document=item.Value.Document,revisionId=item.Value.Revision.Id,indexingStatus="Pending",indexingJobId=item.Value.Job.Id});}catch(ArgumentException e){return Results.ValidationProblem(new Dictionary<string,string[]> { ["document"]=[e.Message] });}});
bases.MapGet("/{knowledgeBaseId:guid}/documents/{documentId:guid}",async(Guid projectId,Guid knowledgeBaseId,Guid documentId,KnowledgeBaseService service,CancellationToken ct)=>(await service.FindDocumentAsync(projectId,knowledgeBaseId,documentId,ct)) is { } item?Results.Ok(item):Results.NotFound());
bases.MapGet("/{knowledgeBaseId:guid}/documents/{documentId:guid}/content",async(Guid projectId,Guid knowledgeBaseId,Guid documentId,KnowledgeBaseService service,CancellationToken ct)=>(await service.ActiveContentAsync(projectId,knowledgeBaseId,documentId,ct)) is { } content?Results.Ok(new {content}):Results.NotFound());
bases.MapPut("/{knowledgeBaseId:guid}/documents/{documentId:guid}/content",async(Guid projectId,Guid knowledgeBaseId,Guid documentId,DocumentContentRequest request,KnowledgeBaseService service,CancellationToken ct)=> {try {var item=await service.ReplaceAsync(projectId,knowledgeBaseId,documentId,request.Content??string.Empty,ct);return item is null?Results.NotFound():Results.Accepted($"/api/projects/{projectId}/knowledge-bases/{knowledgeBaseId}/documents/{documentId}",new {document=item.Value.Document,revisionId=item.Value.Revision.Id,indexingStatus="Pending",indexingJobId=item.Value.Job.Id});}catch(ArgumentException e){return Results.ValidationProblem(new Dictionary<string,string[]> { ["content"]=[e.Message] });}});
bases.MapPatch("/{knowledgeBaseId:guid}/documents/{documentId:guid}",async(Guid projectId,Guid knowledgeBaseId,Guid documentId,DocumentMetadataRequest request,KnowledgeBaseService service,CancellationToken ct)=> {try{return (await service.UpdateDocumentAsync(projectId,knowledgeBaseId,documentId,request.Title??string.Empty,request.Metadata,ct)) is { } item?Results.Ok(item):Results.NotFound();}catch(ArgumentException e){return Results.ValidationProblem(new Dictionary<string,string[]> { ["document"]=[e.Message] });}});
bases.MapDelete("/{knowledgeBaseId:guid}/documents/{documentId:guid}",async(Guid projectId,Guid knowledgeBaseId,Guid documentId,KnowledgeBaseService service,CancellationToken ct)=>await service.DeleteDocumentAsync(projectId,knowledgeBaseId,documentId,ct)?Results.NoContent():Results.NotFound());
app.Run();
public sealed record KnowledgeBaseRequest(string? Name,string? Description);
public sealed record DocumentCreateRequest(string? Title,string? ExternalId,string? Content,JsonElement Metadata);
public sealed record DocumentContentRequest(string? Content);
public sealed record DocumentMetadataRequest(string? Title,JsonElement Metadata);
public sealed record KnowledgeBaseValidationRequest(IReadOnlyList<Guid>? KnowledgeBaseIds);
