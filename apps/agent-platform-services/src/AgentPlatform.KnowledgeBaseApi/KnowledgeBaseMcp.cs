using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using AgentPlatform.Application.Abstractions;
using AgentPlatform.Application.Agents;
using AgentPlatform.Infrastructure.KnowledgeBases;
using AgentPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AgentPlatform.KnowledgeBaseApi;

public static class KnowledgeBaseMcpAuthenticationDefaults { public const string Scheme="KnowledgeBaseAgentApiKey"; public const string AgentId="agent_id"; public const string ProjectId="project_id"; }
public sealed class KnowledgeBaseMcpAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IAgentApiKeyRepository keys) : AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync() { var value=Request.Headers.Authorization.ToString(); if(!value.StartsWith("Bearer ",StringComparison.OrdinalIgnoreCase))return AuthenticateResult.Fail("An agent API key must be supplied."); var auth=await keys.FindAuthenticationAsync(AgentApiKeyService.Hash(value[7..].Trim()),Context.RequestAborted); if(auth is null)return AuthenticateResult.Fail("Invalid API key."); var identity=new ClaimsIdentity([new Claim(KnowledgeBaseMcpAuthenticationDefaults.AgentId,auth.AgentId.ToString()),new Claim(KnowledgeBaseMcpAuthenticationDefaults.ProjectId,auth.ProjectId.ToString())],KnowledgeBaseMcpAuthenticationDefaults.Scheme);return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity),KnowledgeBaseMcpAuthenticationDefaults.Scheme)); }
}
public static class KnowledgeBaseMcp
{
    public static IEndpointRouteBuilder MapKnowledgeBaseMcp(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/mcp", HandleAsync).RequireAuthorization("knowledge-base-mcp");
        return routes;
    }
    private static async Task<IResult> HandleAsync(HttpContext context, PlatformDbContext db, KnowledgeBaseService search, CancellationToken ct)
    {
        using var json=await JsonDocument.ParseAsync(context.Request.Body,cancellationToken:ct); var root=json.RootElement; if(!root.TryGetProperty("method",out var method)||!root.TryGetProperty("id",out var id))return Results.Json(new {jsonrpc="2.0",id=(object?)null,error=new {code=-32600,message="Invalid Request."}});
        if(method.GetString()=="initialize")return Rpc(id,new {protocolVersion="2025-03-26",capabilities=new {tools=new {}},serverInfo=new {name="knowledge-base",version="1.0.0"}});
        var agentId=Guid.Parse(context.User.FindFirstValue(KnowledgeBaseMcpAuthenticationDefaults.AgentId)!); var projectId=Guid.Parse(context.User.FindFirstValue(KnowledgeBaseMcpAuthenticationDefaults.ProjectId)!); var agent=await db.Agents.Include(x=>x.McpToolGrants).Include(x=>x.KnowledgeBaseGrants).SingleOrDefaultAsync(x=>x.Id==agentId&&x.ProjectId==projectId,ct);
        if(agent is null||!agent.McpToolGrants.Any(x=>x.ToolName=="knowledge_base.search"))return Rpc(id,new {content=new[]{new {type="text",text="{\"error\":\"The calling agent is not granted knowledge_base.search.\"}"}},isError=true});
        if(method.GetString()=="tools/list")return Rpc(id,new {tools=new[]{new {name="knowledge_base.search",description="Search authorised project knowledge bases.",inputSchema=new {type="object",properties=new {knowledgeBaseIds=new {type="array"},query=new {type="string"},limit=new {type="integer"}},required=new[]{"knowledgeBaseIds","query"}}}}});
        if(method.GetString()!="tools/call"||!root.TryGetProperty("params",out var p)||!p.TryGetProperty("name",out var name)||name.GetString()!="knowledge_base.search")return Results.Json(new {jsonrpc="2.0",id,error=new {code=-32601,message="Method not found."}});
        try { var a=p.GetProperty("arguments"); var ids=a.GetProperty("knowledgeBaseIds").EnumerateArray().Select(x=>Guid.Parse(x.GetString()!)).ToArray(); if (ids.Except(agent.KnowledgeBaseGrants.Select(x => x.KnowledgeBaseId)).Any()) throw new ArgumentException("The agent is not granted access to one or more requested knowledge bases."); var query=a.GetProperty("query").GetString()??""; var limit=a.TryGetProperty("limit",out var l)&&l.TryGetInt32(out var n)?n:10; var results=await search.SearchAsync(projectId,ids,query,limit,ct); return Rpc(id,new {content=new[]{new {type="text",text=JsonSerializer.Serialize(new {results})}},isError=false}); } catch(ArgumentException e){return Rpc(id,new {content=new[]{new {type="text",text=JsonSerializer.Serialize(new {error=e.Message})}},isError=true});}
    }
    private static IResult Rpc(JsonElement id,object result)=>Results.Json(new {jsonrpc="2.0",id,result});
}
