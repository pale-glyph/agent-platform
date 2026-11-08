"use client";

import { useEffect, useState } from "react";

const navigation = [
  { label: "Overview", icon: "grid" },
  { label: "Projects", icon: "folder" },
  { label: "Agents", icon: "sparkle" },
  { label: "MCP servers", icon: "server" },
  { label: "Instances", icon: "layers" },
  { label: "Event streams", icon: "pulse" },
] as const;

type Project = { id: string; name: string; createdAt: string };

function Icon({ name }: { name: (typeof navigation)[number]["icon"] | "menu" | "close" }) {
  const common = { "aria-hidden": true, viewBox: "0 0 24 24", fill: "none", stroke: "currentColor", strokeWidth: 1.8 };

  if (name === "menu") return <svg {...common}><path d="M4 7h16M4 12h16M4 17h16" /></svg>;
  if (name === "close") return <svg {...common}><path d="m6 6 12 12M18 6 6 18" /></svg>;
  if (name === "grid") return <svg {...common}><rect x="4" y="4" width="6" height="6" rx="1" /><rect x="14" y="4" width="6" height="6" rx="1" /><rect x="4" y="14" width="6" height="6" rx="1" /><rect x="14" y="14" width="6" height="6" rx="1" /></svg>;
  if (name === "folder") return <svg {...common}><path d="M3 7.5A2.5 2.5 0 0 1 5.5 5H10l2 2h6.5A2.5 2.5 0 0 1 21 9.5v8a2.5 2.5 0 0 1-2.5 2.5h-13A2.5 2.5 0 0 1 3 17.5Z" /></svg>;
  if (name === "sparkle") return <svg {...common}><path d="m12 3 1.8 5.2L19 10l-5.2 1.8L12 17l-1.8-5.2L5 10l5.2-1.8ZM19 16l.8 2.2L22 19l-2.2.8L19 22l-.8-2.2L16 19l2.2-.8Z" /></svg>;
  if (name === "server") return <svg {...common}><rect x="3" y="4" width="18" height="6" rx="1.5" /><rect x="3" y="14" width="18" height="6" rx="1.5" /><path d="M7 7h.01M7 17h.01M11 7h6M11 17h6" /></svg>;
  if (name === "layers") return <svg {...common}><path d="m12 3 8 4.5-8 4.5-8-4.5L12 3Z" /><path d="m4 12 8 4.5 8-4.5M4 16.5l8 4.5 8-4.5" /></svg>;
  return <svg {...common}><path d="M4 15h3l2-6 4 10 2-4h5" /><path d="M3 4h18" /></svg>;
}

export default function HomePage() {
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [editingProject, setEditingProject] = useState<Project | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [name, setName] = useState("");

  useEffect(() => {
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") setIsMenuOpen(false);
    };

    window.addEventListener("keydown", closeOnEscape);
    return () => window.removeEventListener("keydown", closeOnEscape);
  }, []);

  const closeMenu = () => setIsMenuOpen(false);

  const loadProjects = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch("/api/projects/");
      if (!response.ok) throw new Error("Projects could not be loaded.");
      setProjects(await response.json() as Project[]);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Projects could not be loaded.");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => { void loadProjects(); }, []);

  const openCreateForm = () => {
    setEditingProject(null);
    setName("");
    setError(null);
    setIsFormOpen(true);
  };

  const openEditForm = (project: Project) => {
    setEditingProject(project);
    setName(project.name);
    setError(null);
    setIsFormOpen(true);
  };

  const readError = async (response: Response) => {
    const body = await response.json().catch(() => null) as { errors?: Record<string, string[]>; title?: string } | null;
    return body?.errors?.name?.[0] ?? body?.title ?? "The project could not be saved.";
  };

  const saveProject = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSubmitting(true);
    setError(null);

    const response = await fetch(editingProject ? `/api/projects/${editingProject.id}` : "/api/projects/", {
      method: editingProject ? "PUT" : "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });

    if (!response.ok) {
      setError(await readError(response));
      setIsSubmitting(false);
      return;
    }

    setIsFormOpen(false);
    setIsSubmitting(false);
    await loadProjects();
  };

  const deleteProject = async (project: Project) => {
    if (!window.confirm(`Delete “${project.name}”? This cannot be undone.`)) return;

    setError(null);
    const response = await fetch(`/api/projects/${project.id}`, { method: "DELETE" });
    if (!response.ok) {
      setError("The project could not be deleted.");
      return;
    }

    await loadProjects();
  };

  return (
    <div className={`app-shell ${isMenuOpen ? "menu-open" : ""} ${isCollapsed ? "menu-collapsed" : ""}`}>
      <button className="menu-backdrop" aria-label="Close navigation menu" onClick={closeMenu} />
      <aside className="sidebar" aria-label="Primary navigation">
        <div className="sidebar-header">
          <a className="brand" href="#overview" onClick={closeMenu} aria-label="Agent Platform overview">
            <span className="brand-mark">AP</span>
            <span className="brand-name">Agent Platform</span>
          </a>
          <button className="icon-button desktop-toggle" onClick={() => setIsCollapsed((value) => !value)} aria-label={isCollapsed ? "Expand navigation" : "Collapse navigation"}>
            <span className="collapse-chevron" aria-hidden="true">‹</span>
          </button>
          <button className="icon-button mobile-close" onClick={closeMenu} aria-label="Close navigation"><Icon name="close" /></button>
        </div>

        <nav>
          <p className="nav-label">Workspace</p>
          <ul>
            {navigation.map((item, index) => (
              <li key={item.label}>
                <a className={index === 0 ? "active" : ""} href={index === 0 ? "#overview" : `#${item.label.toLowerCase().replaceAll(" ", "-")}`} onClick={closeMenu}>
                  <span className="nav-icon"><Icon name={item.icon} /></span>
                  <span className="nav-text">{item.label}</span>
                </a>
              </li>
            ))}
          </ul>
        </nav>

        <div className="sidebar-footer">
          <span className="avatar" aria-hidden="true">OP</span>
          <span className="profile-copy"><strong>Operator</strong><small>Administrator</small></span>
        </div>
      </aside>

      <div className="workspace">
        <header className="topbar">
          <button className="icon-button mobile-menu" onClick={() => setIsMenuOpen(true)} aria-label="Open navigation" aria-expanded={isMenuOpen}><Icon name="menu" /></button>
          <div className="breadcrumb"><span>Workspace</span><span aria-hidden="true">/</span><strong>Overview</strong></div>
          <button className="new-button" type="button" onClick={openCreateForm}>New project</button>
        </header>

        <main id="overview">
          <div className="page-heading">
            <div>
              <p className="eyebrow">Agent Platform</p>
              <h1>Good afternoon, operator.</h1>
              <p>Manage projects, agent definitions, tool access, and active instances from one workspace.</p>
            </div>
            <button className="heading-action" type="button" onClick={openCreateForm}>Create project <span aria-hidden="true">→</span></button>
          </div>

          <section className="project-panel" id="projects" aria-labelledby="projects-heading">
            <div className="panel-heading"><div><p className="eyebrow">Projects</p><h2 id="projects-heading">Your projects</h2></div><span>{projects.length} total</span></div>
            {error && <p className="status-message" role="alert">{error}</p>}
            {isFormOpen && (
              <form className="project-form" onSubmit={saveProject}>
                <label htmlFor="project-name">Project name</label>
                <div><input id="project-name" value={name} maxLength={200} autoFocus onChange={(event) => setName(event.target.value)} placeholder="e.g. Customer support agent" disabled={isSubmitting} /><button className="new-button" disabled={isSubmitting}>{isSubmitting ? "Saving…" : editingProject ? "Save changes" : "Create project"}</button><button className="text-button" type="button" onClick={() => setIsFormOpen(false)} disabled={isSubmitting}>Cancel</button></div>
              </form>
            )}
            {isLoading ? <p className="empty-state" role="status">Loading projects…</p> : projects.length === 0 ? <div className="empty-state"><strong>No projects yet</strong><span>Create a project to begin defining agents and their tools.</span><button className="new-button" type="button" onClick={openCreateForm}>Create your first project</button></div> : (
              <ul className="project-list">
                {projects.map((project) => <li key={project.id}><span className="project-badge">{project.name.slice(0, 1).toUpperCase()}</span><div><strong>{project.name}</strong><small>Created {new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(project.createdAt))}</small></div><div className="project-actions"><button type="button" onClick={() => openEditForm(project)}>Edit</button><button type="button" onClick={() => void deleteProject(project)}>Delete</button></div></li>)}
              </ul>
            )}
          </section>
        </main>
      </div>
    </div>
  );
}
