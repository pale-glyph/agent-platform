import type { ReactNode } from "react";
import type { Project, ProjectTab } from "../../types";

type ProjectWorkspacePageProps = {
  project: Project;
  tab: ProjectTab;
  tabs: ReadonlyArray<{ id: ProjectTab; label: string }>;
  onTabChange: (tab: ProjectTab) => void;
  children: ReactNode;
};

export function ProjectWorkspacePage({
  project,
  tab,
  tabs,
  onTabChange,
  children,
}: ProjectWorkspacePageProps) {
  return (
    <>
      <div className="project-heading">
        <p className="eyebrow">Project</p>
        <h1>{project.name}</h1>
        <p>Configure everything that belongs to this project.</p>
      </div>
      <div className="project-tabs" role="tablist" aria-label="Project sections">
        {tabs.map((item) => (
          <button
            key={item.id}
            type="button"
            role="tab"
            aria-selected={tab === item.id}
            className={tab === item.id ? "active" : ""}
            onClick={() => onTabChange(item.id)}
          >
            {item.label}
          </button>
        ))}
      </div>
      {children}
    </>
  );
}
