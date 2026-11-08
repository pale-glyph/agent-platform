"use client";

import { useEffect, useState } from "react";

type Project = { id: string; name: string; createdAt: string };
type Agent = { id: string; projectId: string; name: string; description: string; systemPrompt: string; createdAt: string };
type Tab = "overview" | "agents" | "servers" | "instances";

const tabs: { id: Tab; label: string }[] = [
  { id: "overview", label: "Overview" }, { id: "agents", label: "Agents" }, { id: "servers", label: "Servers" }, { id: "instances", label: "Instances" },
];

export default function HomePage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [tab, setTab] = useState<Tab>("overview");
  const [agents, setAgents] = useState<Agent[]>([]);
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

  useEffect(() => { void loadProjects(); }, []);
  useEffect(() => { void loadAgents(); }, [selectedProjectId]);

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

  return <div className="app-shell">
    <aside className="sidebar" aria-label="Primary navigation">
      <div className="sidebar-header"><a className="brand" href="#workspace"><span className="brand-mark">AP</span><span className="brand-name">Agent Platform</span></a></div>
      <nav><p className="nav-label">Workspace</p><a className="active" href="#workspace"><span className="nav-icon">▦</span><span>Projects</span></a><a href="#event-streams"><span className="nav-icon">⌁</span><span>Event streams</span></a></nav>
      <section className="project-nav" aria-labelledby="project-nav-title"><div><p className="nav-label" id="project-nav-title">Projects</p><button className="small-add" type="button" onClick={() => setIsProjectFormOpen(true)} aria-label="Create project">+</button></div>
        {isLoading ? <p className="nav-loading">Loading…</p> : projects.map((project) => <button key={project.id} className={project.id === selectedProjectId ? "project-nav-item active" : "project-nav-item"} onClick={() => selectProject(project.id)}><span>{project.name.slice(0, 1).toUpperCase()}</span><strong>{project.name}</strong></button>)}
      </section>
      <div className="sidebar-footer"><span className="avatar">OP</span><span className="profile-copy"><strong>Operator</strong><small>Administrator</small></span></div>
    </aside>

    <div className="workspace" id="workspace"><header className="topbar"><div className="breadcrumb"><span>Projects</span><span aria-hidden="true">/</span><strong>{selectedProject?.name ?? "Select a project"}</strong></div><button className="new-button" type="button" onClick={() => setIsProjectFormOpen(true)}>New project</button></header>
      <main>{message && <p className="status-message" role="alert">{message}</p>}
        {isProjectFormOpen && <form className="project-form project-create-form" onSubmit={createProject}><label htmlFor="project-name">New project name</label><div><input id="project-name" value={projectName} autoFocus maxLength={200} onChange={(event) => setProjectName(event.target.value)} required /><button className="new-button" disabled={isSaving}>Create project</button><button className="text-button" type="button" onClick={() => setIsProjectFormOpen(false)}>Cancel</button></div></form>}
        {!selectedProject ? <div className="empty-state workspace-empty"><strong>Select a project</strong><span>Choose a project from the sidebar or create a new one to begin.</span></div> : <>
          <div className="project-heading"><p className="eyebrow">Project</p><h1>{selectedProject.name}</h1><p>Configure everything that belongs to this project.</p></div>
          <div className="project-tabs" role="tablist" aria-label="Project sections">{tabs.map((item) => <button key={item.id} type="button" role="tab" aria-selected={tab === item.id} className={tab === item.id ? "active" : ""} onClick={() => setTab(item.id)}>{item.label}</button>)}</div>
          {tab === "overview" && <section className="project-panel tab-panel"><h2>Overview</h2><dl className="project-details"><div><dt>Project name</dt><dd>{selectedProject.name}</dd></div><div><dt>Agent definitions</dt><dd>{agents.length}</dd></div><div><dt>Created</dt><dd>{new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(selectedProject.createdAt))}</dd></div></dl></section>}
          {tab === "agents" && <section className="project-panel tab-panel"><div className="panel-heading"><div><p className="eyebrow">Agents</p><h2>Agent definitions</h2></div><button className="new-button" type="button" onClick={() => openAgentForm()}>New agent</button></div>{isAgentFormOpen && <form className="agent-form" onSubmit={saveAgent}><label htmlFor="agent-name">Name</label><input id="agent-name" value={agentName} maxLength={200} onChange={(event) => setAgentName(event.target.value)} required /><label htmlFor="agent-description">Description</label><textarea id="agent-description" value={agentDescription} maxLength={2000} onChange={(event) => setAgentDescription(event.target.value)} required /><label htmlFor="agent-prompt">System prompt</label><textarea id="agent-prompt" value={agentPrompt} maxLength={20000} onChange={(event) => setAgentPrompt(event.target.value)} required /><div><button className="new-button" disabled={isSaving}>{isSaving ? "Saving…" : editingAgent ? "Save changes" : "Create agent"}</button><button className="text-button" type="button" onClick={() => setIsAgentFormOpen(false)}>Cancel</button></div></form>}{agents.length === 0 ? <div className="empty-state"><strong>No agents yet</strong><span>Create an agent definition for {selectedProject.name}.</span></div> : <ul className="project-list">{agents.map((agent) => <li key={agent.id}><span className="project-badge">{agent.name.slice(0, 1).toUpperCase()}</span><div><strong>{agent.name}</strong><small>{agent.description}</small></div><div className="project-actions"><button onClick={() => openAgentForm(agent)}>Edit</button><button onClick={() => void deleteAgent(agent)}>Delete</button></div></li>)}</ul>}</section>}
          {tab === "servers" && <ComingSoon title="MCP servers" text="Connect project-scoped MCP servers and explicitly grant their tools to agent versions." />}
          {tab === "instances" && <ComingSoon title="Instances" text="View the project’s stateful agent instances and their durable conversations." />}
        </>}</main>
    </div>
  </div>;
}

function ComingSoon({ title, text }: { title: string; text: string }) { return <section className="project-panel tab-panel empty-state"><strong>{title}</strong><span>{text}</span><span className="coming-soon">Coming soon</span></section>; }
