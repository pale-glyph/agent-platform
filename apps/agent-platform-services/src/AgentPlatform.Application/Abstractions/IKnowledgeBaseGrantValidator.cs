namespace AgentPlatform.Application.Abstractions;
public interface IKnowledgeBaseGrantValidator { Task<bool> AllBelongToProjectAsync(Guid projectId, IReadOnlyCollection<Guid> knowledgeBaseIds, CancellationToken cancellationToken); }
