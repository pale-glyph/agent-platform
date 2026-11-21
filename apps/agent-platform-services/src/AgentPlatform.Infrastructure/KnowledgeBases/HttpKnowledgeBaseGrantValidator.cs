using System.Net.Http.Json;
using AgentPlatform.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace AgentPlatform.Infrastructure.KnowledgeBases;

public sealed class HttpKnowledgeBaseGrantValidator(HttpClient client, IConfiguration configuration) : IKnowledgeBaseGrantValidator
{
    public async Task<bool> AllBelongToProjectAsync(Guid projectId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count != ids.Distinct().Count()) return false;
        if (ids.Count == 0) return true;
        var baseUrl = configuration["KnowledgeBase:Url"] ?? throw new InvalidOperationException("KnowledgeBase:Url must be configured.");
        var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/internal/projects/{projectId}/knowledge-bases/validate") { Content = JsonContent.Create(new { knowledgeBaseIds = ids }) };
        request.Headers.Add("X-Knowledge-Base-Service-Key", configuration["KnowledgeBase:ServiceKey"] ?? throw new InvalidOperationException("KnowledgeBase:ServiceKey must be configured."));
        using var response = await client.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode && (await response.Content.ReadFromJsonAsync<ValidationResponse>(cancellationToken: cancellationToken))?.Valid == true;
    }
    private sealed record ValidationResponse(bool Valid);
}
