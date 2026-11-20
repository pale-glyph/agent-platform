using AgentPlatform.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
namespace AgentPlatform.Infrastructure.KnowledgeBases;
public sealed class EfKnowledgeBaseGrantValidator(Persistence.PlatformDbContext db) : IKnowledgeBaseGrantValidator
{ public async Task<bool> AllBelongToProjectAsync(Guid projectId, IReadOnlyCollection<Guid> ids, CancellationToken ct) => ids.Count == ids.Distinct().Count() && await db.KnowledgeBases.CountAsync(x => x.ProjectId == projectId && x.DeletedAt == null && ids.Contains(x.Id), ct) == ids.Count; }
