namespace AgentPlatform.Domain.Agents;

public sealed class AgentKnowledgeBaseGrant
{
    private AgentKnowledgeBaseGrant() { }
    public AgentKnowledgeBaseGrant(Guid agentId, Guid knowledgeBaseId) { AgentId = agentId; KnowledgeBaseId = knowledgeBaseId; }
    public Guid AgentId { get; private set; }
    public Guid KnowledgeBaseId { get; private set; }
}
