using AgentPlatform.Application.Abstractions;
using AgentPlatform.Domain.Llm;
namespace AgentPlatform.Application.Llm;
public sealed record LlmConfigurationSummary(Guid Id, string Name, string ProviderUrl, string ModelName, DateTimeOffset CreatedAt);
public sealed class LlmConfigurationService(ILlmConfigurationRepository repository, TimeProvider clock)
{
    public async Task<IReadOnlyList<LlmConfigurationSummary>> ListAsync(CancellationToken ct) => (await repository.ListAsync(ct)).Select(Map).ToArray();
    public async Task<LlmConfigurationSummary> CreateAsync(string name, string url, string model, string key, CancellationToken ct) { var item = new LlmConfiguration(Guid.NewGuid(), name, url, model, key, clock.GetUtcNow()); await repository.AddAsync(item, ct); await repository.SaveChangesAsync(ct); return Map(item); }
    public async Task<LlmConfigurationSummary?> UpdateAsync(Guid id, string name, string url, string model, string? key, CancellationToken ct) { var item = await repository.FindAsync(id, ct); if (item is null) return null; item.Update(name, url, model, key); await repository.SaveChangesAsync(ct); return Map(item); }
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct) { var item = await repository.FindAsync(id, ct); if (item is null) return false; repository.Remove(item); await repository.SaveChangesAsync(ct); return true; }
    private static LlmConfigurationSummary Map(LlmConfiguration item) => new(item.Id, item.Name, item.ProviderUrl, item.ModelName, item.CreatedAt);
}
