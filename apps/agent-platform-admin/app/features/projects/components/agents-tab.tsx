import {
  useState,
  type Dispatch,
  type FormEvent,
  type SetStateAction,
} from "react";
import type {
  Agent,
  KnowledgeBase,
  LlmConfiguration,
  McpServer,
  McpTool,
  Project,
} from "../../../types";

type Props = {
  project: Project;
  agents: Agent[];
  llmConfigurations: LlmConfiguration[];
  mcpServers: McpServer[];
  knowledgeBases: KnowledgeBase[];
  editingAgent: Agent | null;
  isFormOpen: boolean;
  isSaving: boolean;
  name: string;
  description: string;
  prompt: string;
  llmConfigurationId: string;
  toolsByServer: Record<string, McpTool[]>;
  selectedToolKeys: string[];
  selectedKnowledgeBaseIds: string[];
  isLoadingTools: boolean;
  onOpenForm: (agent?: Agent) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onCancel: () => void;
  onDelete: (agent: Agent) => void;
  onManageApiKeys: (agent: Agent) => void;
  onNameChange: (value: string) => void;
  onDescriptionChange: (value: string) => void;
  onPromptChange: (value: string) => void;
  onLlmConfigurationChange: (value: string) => void;
  onSelectedToolKeysChange: Dispatch<SetStateAction<string[]>>;
  onSelectedKnowledgeBaseIdsChange: Dispatch<SetStateAction<string[]>>;
};

export function AgentsTab(p: Props) {
  const [copiedId, setCopiedId] = useState<string | null>(null);
  return (
    <section className="project-panel tab-panel">
      <div className="panel-heading">
        <div>
          <p className="eyebrow">Agents</p>
          <h2>Agent definitions</h2>
        </div>
        <button
          className="new-button"
          type="button"
          onClick={() => p.onOpenForm()}
        >
          New agent
        </button>
      </div>
      {p.isFormOpen && (
        <form className="agent-form" onSubmit={p.onSubmit}>
          <label htmlFor="agent-name">Name</label>
          <input
            id="agent-name"
            value={p.name}
            maxLength={200}
            onChange={(e) => p.onNameChange(e.target.value)}
            required
          />
          <label htmlFor="agent-description">Description</label>
          <textarea
            id="agent-description"
            value={p.description}
            maxLength={2000}
            onChange={(e) => p.onDescriptionChange(e.target.value)}
            required
          />
          <label htmlFor="agent-prompt">System prompt</label>
          <textarea
            id="agent-prompt"
            value={p.prompt}
            maxLength={20000}
            onChange={(e) => p.onPromptChange(e.target.value)}
            required
          />
          <label htmlFor="agent-llm">LLM configuration</label>
          <select
            id="agent-llm"
            value={p.llmConfigurationId}
            onChange={(e) => p.onLlmConfigurationChange(e.target.value)}
          >
            <option value="">
              No LLM selected — agent will be misconfigured
            </option>
            {p.llmConfigurations.map((x) => (
              <option key={x.id} value={x.id}>
                {x.name} ({x.modelName})
              </option>
            ))}
          </select>
          <fieldset className="mcp-tool-options">
            <legend>Knowledge-base access</legend>
            <p>Select the knowledge bases this agent may retrieve from.</p>
            {p.knowledgeBases.length === 0 ? (
              <span>No knowledge bases are available.</span>
            ) : (
              p.knowledgeBases.map((x) => (
                <label className="permission-option" key={x.id}>
                  <input
                    type="checkbox"
                    checked={p.selectedKnowledgeBaseIds.includes(x.id)}
                    onChange={(e) => {
                      const checked = e.target.checked;
                      p.onSelectedKnowledgeBaseIdsChange((ids) =>
                        checked
                          ? [...ids, x.id]
                          : ids.filter((id) => id !== x.id),
                      );
                    }}
                  />
                  <span>{x.name}</span>
                </label>
              ))
            )}
          </fieldset>
          <fieldset className="mcp-tool-options">
            <legend>MCP tools</legend>
            <p>
              Select only the tools this agent may call. Access is
              deny-by-default.
            </p>
            {p.isLoadingTools ? (
              <span role="status">Loading tools…</span>
            ) : p.mcpServers.length === 0 ? (
              <span>No MCP servers are configured for this project.</span>
            ) : (
              p.mcpServers.map((server) => (
                <div className="mcp-tool-server" key={server.id}>
                  <strong>{server.name}</strong>
                  {(p.toolsByServer[server.id] ?? []).length === 0 ? (
                    <small>No tools available.</small>
                  ) : (
                    (p.toolsByServer[server.id] ?? []).map((tool) => {
                      const item = `${server.id}:${tool.name}`;
                      return (
                        <label className="permission-option" key={item}>
                          <input
                            type="checkbox"
                            checked={p.selectedToolKeys.includes(item)}
                            onChange={(e) => {
                              const checked = e.target.checked;
                              p.onSelectedToolKeysChange((ids) =>
                                checked
                                  ? [...ids, item]
                                  : ids.filter((id) => id !== item),
                              );
                            }}
                          />
                          <span>
                            {tool.name}
                            {tool.description && (
                              <small>{tool.description}</small>
                            )}
                          </span>
                        </label>
                      );
                    })
                  )}
                </div>
              ))
            )}
          </fieldset>
          <div>
            <button className="new-button" disabled={p.isSaving}>
              {p.isSaving
                ? "Saving…"
                : p.editingAgent
                  ? "Save changes"
                  : "Create agent"}
            </button>
            <button className="text-button" type="button" onClick={p.onCancel}>
              Cancel
            </button>
            <span role="status">{p.isSaving ? "Saving agent…" : ""}</span>
          </div>
        </form>
      )}
      {p.agents.length === 0 ? (
        <div className="empty-state">
          <strong>No agents yet</strong>
          <span>Create an agent definition for {p.project.name}.</span>
        </div>
      ) : (
        <ul className="project-list">
          {p.agents.map((agent) => (
            <li key={agent.id}>
              <span className="project-badge">
                {agent.name.slice(0, 1).toUpperCase()}
              </span>
              <div>
                <strong>{agent.name}</strong>
                {!agent.llmConfigurationId && (
                  <span className="agent-misconfigured">
                    Misconfigured: no LLM selected
                  </span>
                )}
                <small>{agent.description}</small>
                <span className="agent-uuid">
                  <span>UUID</span>
                  <code>{agent.id}</code>
                  <button
                    type="button"
                    onClick={() =>
                      void navigator.clipboard
                        .writeText(agent.id)
                        .then(() => setCopiedId(agent.id))
                    }
                  >
                    {copiedId === agent.id ? "Copied" : "Copy UUID"}
                  </button>
                </span>
                <small>
                  {agent.mcpToolGrants.length} MCP tools and{" "}
                  {(agent.knowledgeBaseIds ?? []).length} knowledge bases
                  granted
                </small>
              </div>
              <div className="project-actions">
                <button onClick={() => p.onManageApiKeys(agent)}>
                  API keys
                </button>
                <button onClick={() => p.onOpenForm(agent)}>Edit</button>
                <button onClick={() => p.onDelete(agent)}>Delete</button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
