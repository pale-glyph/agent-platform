using AgentPlatform.Domain.Llm;
namespace AgentPlatform.Application.Abstractions;
public interface ILlmConfigurationRepository { Task<IReadOnlyList<LlmConfiguration>> ListAsync(CancellationToken cancellationToken); Task<LlmConfiguration?> FindAsync(Guid id, CancellationToken cancellationToken); Task AddAsync(LlmConfiguration item, CancellationToken cancellationToken); void Remove(LlmConfiguration item); Task SaveChangesAsync(CancellationToken cancellationToken); }
