"use client";

import { useEffect, useState } from "react";

type Project = { id: string; name: string; createdAt: string };
type Agent = { id: string; projectId: string; name: string; description: string; systemPrompt: string; createdAt: string };
type LlmConfiguration = { id: string; name: string; providerUrl: string; modelName: string; createdAt: string };
type McpServer = { id: string; projectId: string; name: string; endpoint: string; cacheTtlSeconds: number; security: "None" | "ApiKey"; cacheExpiresAt: string | null; createdAt: string };
type McpTool = { name: string; description?: string; inputSchema?: unknown; annotations?: Record<string, unknown> };
type Tab = "overview" | "agents" | "servers" | "instances";

const tabs: { id: Tab; label: string }[] = [
  { id: "overview", label: "Overview" }, { id: "agents", label: "Agents" }, { id: "servers", label: "Servers" }, { id: "instances", label: "Instances" },
];

export default function HomePage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [tab, setTab] = useState<Tab>("overview");
  const [agents, setAgents] = useState<Agent[]>([]);
  const [mcpServers, setMcpServers] = useState<McpServer[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [isProjectFormOpen, setIsProjectFormOpen] = useState(false);
  const [projectName, setProjectName] = useState("");
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null);
  const [isAgentFormOpen, setIsAgentFormOpen] = useState(false);
  const [agentName, setAgentName] = useState("");
  const [agentDescription, setAgentDescription] = useState("");
  const [agentPrompt, setAgentPrompt] = useState("");
  const [editingMcpServer, setEditingMcpServer] = useState<McpServer | null>(null);
  const [isMcpServerFormOpen, setIsMcpServerFormOpen] = useState(false);
  const [mcpServerName, setMcpServerName] = useState("");
  const [mcpEndpoint, setMcpEndpoint] = useState("");
  const [mcpCacheTtlSeconds, setMcpCacheTtlSeconds] = useState("300");
  const [mcpSecurity, setMcpSecurity] = useState<"None" | "ApiKey">("None");
  const [mcpApiKey, setMcpApiKey] = useState("");
  const [viewingToolsFor, setViewingToolsFor] = useState<McpServer | null>(null);
  const [mcpTools, setMcpTools] = useState<McpTool[]>([]);
  const [isLoadingMcpTools, setIsLoadingMcpTools] = useState(false);
  const [mcpToolsError, setMcpToolsError] = useState<string | null>(null);
  const [llmConfigurations, setLlmConfigurations] = useState<LlmConfiguration[]>([]);
  const [isLlmFormOpen, setIsLlmFormOpen] = useState(false);
  const [editingLlm, setEditingLlm] = useState<LlmConfiguration | null>(null);
  const [llmName, setLlmName] = useState("");
  const [providerUrl, setProviderUrl] = useState("");
  const [modelName, setModelName] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [page, setPage] = useState<"projects" | "llm">("projects");

  const selectedProject = projects.find((project) => project.id === selectedProjectId);

  const loadProjects = async () => {
    setIsLoading(true);
    try {
      const response = await fetch("/api/projects/");
      if (!response.ok) throw new Error("Projects could not be loaded.");
      const items = await response.json() as Project[];
      setProjects(items);
      setSelectedProjectId((current) => current || items[0]?.id || "");
    } catch (error) { setMessage(error instanceof Error ? error.message : "Projects could not be loaded."); }
    finally { setIsLoading(false); }
  };

  const loadAgents = async () => {
    if (!selectedProjectId) { setAgents([]); return; }
    try {
      const response = await fetch(`/api/projects/${selectedProjectId}/agents/`);
      if (!response.ok) throw new Error("Agents could not be loaded.");
      setAgents(await response.json() as Agent[]);
    } catch (error) { setMessage(error instanceof Error ? error.message : "Agents could not be loaded."); }
  };
  const loadMcpServers = async () => {
    if (!selectedProjectId) { setMcpServers([]); return; }
    try {
      const response = await fetch(`/api/projects/${selectedProjectId}/mcp-servers/`);
      if (!response.ok) throw new Error("MCP servers could not be loaded.");
      setMcpServers(await response.json() as McpServer[]);
    } catch (error) { setMessage(error instanceof Error ? error.message : "MCP servers could not be loaded."); }
  };

  useEffect(() => { void loadProjects(); }, []);
  useEffect(() => { void loadAgents(); }, [selectedProjectId]);
  useEffect(() => { void loadMcpServers(); }, [selectedProjectId]);
  const loadLlmConfigurations = async () => { try { const response = await fetch("/api/llm-configurations/"); if (!response.ok) throw new Error("LLM configurations could not be loaded."); setLlmConfigurations(await response.json() as LlmConfiguration[]); } catch (error) { setMessage(error instanceof Error ? error.message : "LLM configurations could not be loaded."); } };
  useEffect(() => { void loadLlmConfigurations(); }, []);

  const selectProject = (projectId: string) => { setSelectedProjectId(projectId); setTab("overview"); setMessage(null); };
  const readError = async (response: Response) => {
    const body = await response.json().catch(() => null) as { errors?: Record<string, string[]>; title?: string } | null;
    return body?.errors ? Object.values(body.errors).flat()[0] : body?.title ?? "The request could not be saved.";
  };

  const createProject = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); setIsSaving(true); setMessage(null);
    const response = await fetch("/api/projects/", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: projectName }) });
    if (!response.ok) { setMessage(await readError(response)); setIsSaving(false); return; }
    const project = await response.json() as Project;
    setProjects((items) => [...items, project].sort((a, b) => a.name.localeCompare(b.name)));
    selectProject(project.id); setProjectName(""); setIsProjectFormOpen(false); setIsSaving(false);
  };

  const openAgentForm = (agent?: Agent) => {
    setEditingAgent(agent ?? null); setAgentName(agent?.name ?? ""); setAgentDescription(agent?.description ?? ""); setAgentPrompt(agent?.systemPrompt ?? ""); setIsAgentFormOpen(true); setMessage(null);
  };

  const saveAgent = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!selectedProjectId) return; setIsSaving(true); setMessage(null);
    const response = await fetch(editingAgent ? `/api/projects/${selectedProjectId}/agents/${editingAgent.id}` : `/api/projects/${selectedProjectId}/agents/`, { method: editingAgent ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: agentName, description: agentDescription, systemPrompt: agentPrompt }) });
    if (!response.ok) { setMessage(await readError(response)); setIsSaving(false); return; }
    setIsAgentFormOpen(false); setIsSaving(false); await loadAgents();
  };

  const deleteAgent = async (agent: Agent) => {
    if (!selectedProjectId || !window.confirm(`Delete “${agent.name}”? This cannot be undone.`)) return;
    const response = await fetch(`/api/projects/${selectedProjectId}/agents/${agent.id}`, { method: "DELETE" });
    if (!response.ok) { setMessage("The agent could not be deleted."); return; }
    await loadAgents();
  };

  const openMcpServerForm = (server?: McpServer) => {
    setEditingMcpServer(server ?? null); setMcpServerName(server?.name ?? ""); setMcpEndpoint(server?.endpoint ?? ""); setMcpCacheTtlSeconds(String(server?.cacheTtlSeconds ?? 300)); setMcpSecurity(server?.security ?? "None"); setMcpApiKey(""); setIsMcpServerFormOpen(true); setMessage(null);
  };
  const saveMcpServer = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!selectedProjectId) return; setIsSaving(true); setMessage(null);
    const response = await fetch(editingMcpServer ? `/api/projects/${selectedProjectId}/mcp-servers/${editingMcpServer.id}` : `/api/projects/${selectedProjectId}/mcp-servers/`, { method: editingMcpServer ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: mcpServerName, endpoint: mcpEndpoint, cacheTtlSeconds: Number(mcpCacheTtlSeconds), security: mcpSecurity, apiKey: mcpApiKey || undefined }) });
    if (!response.ok) { setMessage(await readError(response)); setIsSaving(false); return; }
    setIsSaving(false); setIsMcpServerFormOpen(false); await loadMcpServers();
  };
  const deleteMcpServer = async (server: McpServer) => {
    if (!selectedProjectId || !window.confirm(`Delete “${server.name}”? This cannot be undone.`)) return;
    const response = await fetch(`/api/projects/${selectedProjectId}/mcp-servers/${server.id}`, { method: "DELETE" });
    if (!response.ok) { setMessage("The MCP server could not be deleted."); return; }
    await loadMcpServers();
  };
  const clearMcpServerCache = async (server: McpServer) => {
    if (!selectedProjectId || !window.confirm(`Clear the cached tools for “${server.name}”?`)) return;
    const response = await fetch(`/api/projects/${selectedProjectId}/mcp-servers/${server.id}/cache/clear`, { method: "POST" });
    if (!response.ok) { setMessage("The MCP server cache could not be cleared."); return; }
    setMessage(`Cleared the cached tools for ${server.name}.`); await loadMcpServers();
  };
  const viewMcpTools = async (server: McpServer) => {
    if (!selectedProjectId) return;
    setViewingToolsFor(server); setMcpTools([]); setMcpToolsError(null); setIsLoadingMcpTools(true);
    try {
      const response = await fetch(`/api/projects/${selectedProjectId}/mcp-servers/${server.id}`);
      if (!response.ok) throw new Error(await readError(response));
      const result = await response.json() as { tools?: McpTool[] } | McpTool[];
      const tools = Array.isArray(result) ? result : result.tools;
      if (!tools || !Array.isArray(tools)) throw new Error("The MCP server returned an invalid tools list.");
      setMcpTools(tools);
    } catch (error) { setMcpToolsError(error instanceof Error ? error.message : "MCP tools could not be loaded."); }
    finally { setIsLoadingMcpTools(false); }
  };

  const openLlmForm = (item?: LlmConfiguration) => { setEditingLlm(item ?? null); setLlmName(item?.name ?? ""); setProviderUrl(item?.providerUrl ?? ""); setModelName(item?.modelName ?? ""); setApiKey(""); setIsLlmFormOpen(true); };
  const saveLlm = async (event: React.FormEvent<HTMLFormElement>) => { event.preventDefault(); setIsSaving(true); const response = await fetch(editingLlm ? `/api/llm-configurations/${editingLlm.id}` : "/api/llm-configurations/", { method: editingLlm ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: llmName, providerUrl, modelName, apiKey: apiKey || undefined }) }); if (!response.ok) { setMessage(await readError(response)); setIsSaving(false); return; } setIsSaving(false); setIsLlmFormOpen(false); await loadLlmConfigurations(); };
  const deleteLlm = async (item: LlmConfiguration) => { if (!window.confirm(`Delete “${item.name}”? This cannot be undone.`)) return; const response = await fetch(`/api/llm-configurations/${item.id}`, { method: "DELETE" }); if (!response.ok) { setMessage("The LLM configuration could not be deleted."); return; } await loadLlmConfigurations(); };

  return <div className="app-shell">
    <aside className="sidebar" aria-label="Primary navigation">
      <div className="sidebar-header"><a className="brand" href="#workspace"><span className="brand-mark">AP</span><span className="brand-name">Agent Platform</span></a></div>
      <nav><p className="nav-label">Workspace</p><button className={page === "projects" ? "active" : ""} onClick={() => setPage("projects")}><span className="nav-icon">▦</span><span>Projects</span></button><button className={page === "llm" ? "active" : ""} onClick={() => setPage("llm")}><span className="nav-icon">✦</span><span>LLM configurations</span></button><a href="#event-streams"><span className="nav-icon">⌁</span><span>Event streams</span></a></nav>
      {page === "projects" && <section className="project-nav" aria-labelledby="project-nav-title"><div><p className="nav-label" id="project-nav-title">Projects</p><button className="small-add" type="button" onClick={() => setIsProjectFormOpen(true)} aria-label="Create project">+</button></div>
        {isLoading ? <p className="nav-loading">Loading…</p> : projects.map((project) => <button key={project.id} className={project.id === selectedProjectId ? "project-nav-item active" : "project-nav-item"} onClick={() => selectProject(project.id)}><span>{project.name.slice(0, 1).toUpperCase()}</span><strong>{project.name}</strong></button>)}
      </section>}
      <div className="sidebar-footer"><span className="avatar">OP</span><span className="profile-copy"><strong>Operator</strong><small>Administrator</small></span></div>
    </aside>

    <div className="workspace" id="workspace"><header className="topbar"><div className="breadcrumb"><span>{page === "projects" ? "Projects" : "Platform"}</span>{page === "projects" && <><span aria-hidden="true">/</span><strong>{selectedProject?.name ?? "Select a project"}</strong></>}</div>{page === "projects" ? <button className="new-button" type="button" onClick={() => setIsProjectFormOpen(true)}>New project</button> : <button className="new-button" type="button" onClick={() => openLlmForm()}>New configuration</button>}</header>
      <main>{message && <p className="status-message" role="alert">{message}</p>}
        {page === "projects" && isProjectFormOpen && <form className="project-form project-create-form" onSubmit={createProject}><label htmlFor="project-name">New project name</label><div><input id="project-name" value={projectName} autoFocus maxLength={200} onChange={(event) => setProjectName(event.target.value)} required /><button className="new-button" disabled={isSaving}>Create project</button><button className="text-button" type="button" onClick={() => setIsProjectFormOpen(false)}>Cancel</button></div></form>}
        {page === "projects" && (!selectedProject ? <div className="empty-state workspace-empty"><strong>Select a project</strong><span>Choose a project from the sidebar or create a new one to begin.</span></div> : <>
          <div className="project-heading"><p className="eyebrow">Project</p><h1>{selectedProject.name}</h1><p>Configure everything that belongs to this project.</p></div>
          <div className="project-tabs" role="tablist" aria-label="Project sections">{tabs.map((item) => <button key={item.id} type="button" role="tab" aria-selected={tab === item.id} className={tab === item.id ? "active" : ""} onClick={() => setTab(item.id)}>{item.label}</button>)}</div>
          {tab === "overview" && <section className="project-panel tab-panel"><h2>Overview</h2><dl className="project-details"><div><dt>Project name</dt><dd>{selectedProject.name}</dd></div><div><dt>Agent definitions</dt><dd>{agents.length}</dd></div><div><dt>MCP servers</dt><dd>{mcpServers.length}</dd></div><div><dt>Created</dt><dd>{new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(selectedProject.createdAt))}</dd></div></dl></section>}
          {tab === "agents" && <section className="project-panel tab-panel"><div className="panel-heading"><div><p className="eyebrow">Agents</p><h2>Agent definitions</h2></div><button className="new-button" type="button" onClick={() => openAgentForm()}>New agent</button></div>{isAgentFormOpen && <form className="agent-form" onSubmit={saveAgent}><label htmlFor="agent-name">Name</label><input id="agent-name" value={agentName} maxLength={200} onChange={(event) => setAgentName(event.target.value)} required /><label htmlFor="agent-description">Description</label><textarea id="agent-description" value={agentDescription} maxLength={2000} onChange={(event) => setAgentDescription(event.target.value)} required /><label htmlFor="agent-prompt">System prompt</label><textarea id="agent-prompt" value={agentPrompt} maxLength={20000} onChange={(event) => setAgentPrompt(event.target.value)} required /><div><button className="new-button" disabled={isSaving}>{isSaving ? "Saving…" : editingAgent ? "Save changes" : "Create agent"}</button><button className="text-button" type="button" onClick={() => setIsAgentFormOpen(false)}>Cancel</button></div></form>}{agents.length === 0 ? <div className="empty-state"><strong>No agents yet</strong><span>Create an agent definition for {selectedProject.name}.</span></div> : <ul className="project-list">{agents.map((agent) => <li key={agent.id}><span className="project-badge">{agent.name.slice(0, 1).toUpperCase()}</span><div><strong>{agent.name}</strong><small>{agent.description}</small></div><div className="project-actions"><button onClick={() => openAgentForm(agent)}>Edit</button><button onClick={() => void deleteAgent(agent)}>Delete</button></div></li>)}</ul>}</section>}
          {tab === "servers" && <section className="project-panel tab-panel"><div className="panel-heading"><div><p className="eyebrow">MCP servers</p><h2>Project tool integrations</h2></div><button className="new-button" type="button" onClick={() => openMcpServerForm()}>New server</button></div>{isMcpServerFormOpen && <form className="agent-form" onSubmit={saveMcpServer}><label htmlFor="mcp-server-name">Name</label><input id="mcp-server-name" value={mcpServerName} maxLength={200} onChange={(event) => setMcpServerName(event.target.value)} required /><label htmlFor="mcp-endpoint">Server endpoint</label><input id="mcp-endpoint" type="url" value={mcpEndpoint} maxLength={2000} onChange={(event) => setMcpEndpoint(event.target.value)} required /><label htmlFor="mcp-cache-ttl">Tool cache TTL (seconds)</label><input id="mcp-cache-ttl" type="number" min="1" max="86400" value={mcpCacheTtlSeconds} onChange={(event) => setMcpCacheTtlSeconds(event.target.value)} required /><label htmlFor="mcp-security">Security</label><select id="mcp-security" value={mcpSecurity} onChange={(event) => setMcpSecurity(event.target.value as "None" | "ApiKey")}><option value="None">None</option><option value="ApiKey">API key</option></select>{mcpSecurity === "ApiKey" && <><label htmlFor="mcp-api-key">API key {editingMcpServer && <small>(leave blank to keep current key)</small>}</label><input id="mcp-api-key" type="password" value={mcpApiKey} onChange={(event) => setMcpApiKey(event.target.value)} required={!editingMcpServer} /></>}<div><button className="new-button" disabled={isSaving}>{isSaving ? "Saving…" : editingMcpServer ? "Save changes" : "Create server"}</button><button className="text-button" type="button" onClick={() => setIsMcpServerFormOpen(false)}>Cancel</button></div></form>}{mcpServers.length === 0 ? <div className="empty-state"><strong>No MCP servers yet</strong><span>Add a project-scoped MCP server to discover and cache its tools.</span></div> : <ul className="project-list">{mcpServers.map((server) => <li key={server.id}><span className="project-badge">⌘</span><div><strong>{server.name}</strong><small>{server.security === "ApiKey" ? "API key" : "No auth"} · cache {server.cacheTtlSeconds}s · {server.endpoint}</small></div><div className="project-actions"><button onClick={() => void viewMcpTools(server)}>View tools</button><button onClick={() => void clearMcpServerCache(server)}>Clear cache</button><button onClick={() => openMcpServerForm(server)}>Edit</button><button onClick={() => void deleteMcpServer(server)}>Delete</button></div></li>)}</ul>}</section>}
          {tab === "instances" && <ComingSoon title="Instances" text="View the project’s stateful agent instances and their durable conversations." />}
        </>) }
        {page === "llm" && <section className="project-panel tab-panel" id="llm-configurations"><div className="panel-heading"><div><p className="eyebrow">Platform</p><h2>LLM configurations</h2></div><button className="new-button" type="button" onClick={() => openLlmForm()}>New configuration</button></div>{isLlmFormOpen && <form className="agent-form" onSubmit={saveLlm}><label htmlFor="llm-name">Name</label><input id="llm-name" value={llmName} onChange={(event) => setLlmName(event.target.value)} required /><label htmlFor="provider-url">Provider URL</label><input id="provider-url" type="url" value={providerUrl} onChange={(event) => setProviderUrl(event.target.value)} required /><label htmlFor="model-name">Model name</label><input id="model-name" value={modelName} onChange={(event) => setModelName(event.target.value)} required /><label htmlFor="api-key">API key {editingLlm && <small>(leave blank to keep current key)</small>}</label><input id="api-key" type="password" value={apiKey} onChange={(event) => setApiKey(event.target.value)} required={!editingLlm} /><div><button className="new-button" disabled={isSaving}>{isSaving ? "Saving…" : "Save configuration"}</button><button className="text-button" type="button" onClick={() => setIsLlmFormOpen(false)}>Cancel</button></div></form>}{llmConfigurations.length === 0 ? <div className="empty-state"><strong>No LLM configurations yet</strong><span>Add a provider configuration to make models available to agents.</span></div> : <ul className="project-list">{llmConfigurations.map((item) => <li key={item.id}><span className="project-badge">✦</span><div><strong>{item.name}</strong><small>{item.modelName} · {item.providerUrl}</small></div><div className="project-actions"><button onClick={() => openLlmForm(item)}>Edit</button><button onClick={() => void deleteLlm(item)}>Delete</button></div></li>)}</ul>}</section>}
        </main>
    </div>
    {viewingToolsFor && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setViewingToolsFor(null); }}><section className="tools-modal" role="dialog" aria-modal="true" aria-labelledby="mcp-tools-title"><header className="tools-modal-heading"><div><p className="eyebrow">MCP server tools</p><h2 id="mcp-tools-title">{viewingToolsFor.name}</h2><p>{viewingToolsFor.endpoint}</p></div><button className="modal-close" type="button" onClick={() => setViewingToolsFor(null)} aria-label="Close tools dialog">×</button></header><div className="tools-modal-content" aria-live="polite">{isLoadingMcpTools ? <div className="empty-state"><strong>Loading tools…</strong><span>Fetching the server’s tool definitions.</span></div> : mcpToolsError ? <p className="status-message" role="alert">{mcpToolsError}</p> : mcpTools.length === 0 ? <div className="empty-state"><strong>No tools found</strong><span>This server did not return any tool definitions.</span></div> : <div className="tools-table-wrap"><table className="tools-table"><thead><tr><th scope="col">Name</th><th scope="col">Description</th><th scope="col">Input schema</th></tr></thead><tbody>{mcpTools.map((tool, index) => <tr key={`${tool.name}-${index}`}><th scope="row">{tool.name}</th><td>{tool.description || "—"}</td><td><pre>{JSON.stringify(tool.inputSchema ?? {}, null, 2)}</pre></td></tr>)}</tbody></table></div>}</div><footer className="tools-modal-footer"><button className="text-button" type="button" onClick={() => setViewingToolsFor(null)}>Close</button></footer></section></div>}
  </div>;
}

function ComingSoon({ title, text }: { title: string; text: string }) { return <section className="project-panel tab-panel empty-state"><strong>{title}</strong><span>{text}</span><span className="coming-soon">Coming soon</span></section>; }
