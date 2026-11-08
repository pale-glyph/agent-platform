import type { AdminPage, Project } from "../types";

type AppSidebarProps = {
  page: AdminPage;
  projects: Project[];
  selectedProjectId: string;
  isLoading: boolean;
  onPageChange: (page: AdminPage) => void;
  onUsageSelect: () => void;
  onProjectSelect: (projectId: string) => void;
  onCreateProject: () => void;
};

export function AppSidebar({
  page, projects, selectedProjectId, isLoading, onPageChange, onUsageSelect,
  onProjectSelect, onCreateProject,
}: AppSidebarProps) {
  return <aside className="sidebar" aria-label="Primary navigation">
    <div className="sidebar-header"><a className="brand" href="#workspace"><span className="brand-mark">AP</span><span className="brand-name">Agent Platform</span></a></div>
    <nav>
      <p className="nav-label">Workspace</p>
      <button className={page === "projects" ? "active" : ""} onClick={() => onPageChange("projects")}><span className="nav-icon">▦</span><span>Projects</span></button>
      <button className={page === "chat" ? "active" : ""} onClick={() => onPageChange("chat")}><span className="nav-icon">◌</span><span>Chat with Jim</span></button>
      <button className={page === "llm" ? "active" : ""} onClick={() => onPageChange("llm")}><span className="nav-icon">✦</span><span>LLM configurations</span></button>
      <button className={page === "usage" ? "active" : ""} onClick={onUsageSelect}><span className="nav-icon">◫</span><span>Usage</span></button>
    </nav>
    {page === "projects" && <section className="project-nav" aria-labelledby="project-nav-title">
      <div><p className="nav-label" id="project-nav-title">Projects</p><button className="small-add" type="button" onClick={onCreateProject} aria-label="Create project">+</button></div>
      {isLoading ? <p className="nav-loading">Loading…</p> : projects.map((project) => <button key={project.id} className={project.id === selectedProjectId ? "project-nav-item active" : "project-nav-item"} onClick={() => onProjectSelect(project.id)}><span>{project.name.slice(0, 1).toUpperCase()}</span><strong>{project.name}</strong></button>)}
    </section>}
    <div className="sidebar-footer"><span className="avatar">OP</span><span className="profile-copy"><strong>Operator</strong><small>Administrator</small></span></div>
  </aside>;
}
