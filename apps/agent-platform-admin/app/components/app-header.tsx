import type { AdminPage, Project } from "../types";

type AppHeaderProps = {
  page: AdminPage;
  selectedProject?: Project;
  onCreateProject: () => void;
  onCreateLlm: () => void;
};

export function AppHeader({
  page,
  selectedProject,
  onCreateProject,
  onCreateLlm,
}: AppHeaderProps) {
  const label =
    page === "projects"
      ? "Projects"
      : page === "chat"
        ? "Chat with Jim"
        : page === "usage"
          ? "Usage"
          : "Platform";

  return (
    <header className="topbar">
      <div className="breadcrumb">
        <span>{label}</span>
        {page === "projects" && (
          <>
            <span aria-hidden="true">/</span>
            <strong>{selectedProject?.name ?? "Select a project"}</strong>
          </>
        )}
      </div>
      {page === "projects" ? (
        <button className="new-button" type="button" onClick={onCreateProject}>
          New project
        </button>
      ) : page === "llm" ? (
        <button className="new-button" type="button" onClick={onCreateLlm}>
          New configuration
        </button>
      ) : null}
    </header>
  );
}
