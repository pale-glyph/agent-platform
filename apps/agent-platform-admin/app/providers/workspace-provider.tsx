"use client";

import { useCallback, useMemo, useState } from "react";
import type { FormEvent, ReactNode } from "react";
import { WorkspaceNavigationProvider } from "../contexts/workspace-navigation-context";
import { useProjects } from "../hooks/use-projects";
import type { AdminPage, ProjectTab } from "../types";

type WorkspaceProviderProps = { children: ReactNode };

export function WorkspaceProvider({ children }: WorkspaceProviderProps) {
  const [message, setMessage] = useState<string | null>(null);
  const [page, setPage] = useState<AdminPage>("projects");
  const [tab, setTab] = useState<ProjectTab>("overview");
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [isProjectFormOpen, setIsProjectFormOpen] = useState(false);
  const [projectName, setProjectName] = useState("");
  const [isSavingProject, setIsSavingProject] = useState(false);

  const reportMessage = useCallback((value: string | null) => {
    setMessage(value);
  }, []);
  const { projects, isLoading, create } = useProjects(reportMessage);
  const activeProjectId = selectedProjectId || projects[0]?.id || "";
  const selectedProject = projects.find((project) => project.id === activeProjectId);

  const selectProject = useCallback((projectId: string) => {
    setSelectedProjectId(projectId);
    setTab("overview");
    setMessage(null);
  }, []);

  const openProjectForm = useCallback(() => {
    setIsProjectFormOpen(true);
  }, []);

  const closeProjectForm = useCallback(() => {
    setIsProjectFormOpen(false);
  }, []);

  const createProject = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      setIsSavingProject(true);
      setMessage(null);
      try {
        const project = await create(projectName);
        selectProject(project.id);
        setProjectName("");
        setIsProjectFormOpen(false);
      } catch (error) {
        setMessage(
          error instanceof Error ? error.message : "The project could not be created.",
        );
      } finally {
        setIsSavingProject(false);
      }
    },
    [create, projectName, selectProject],
  );

  const value = useMemo(
    () => ({
      page,
      setPage,
      projects,
      selectedProject,
      selectedProjectId: activeProjectId,
      isLoadingProjects: isLoading,
      selectProject,
      openProjectForm,
      openUsage: () => setPage("usage"),
      tab,
      setTab,
      message,
      reportMessage,
      isProjectFormOpen,
      projectName,
      isSavingProject,
      setProjectName,
      createProject,
      closeProjectForm,
    }),
    [
      createProject,
      isLoading,
      isProjectFormOpen,
      isSavingProject,
      message,
      page,
      projectName,
      projects,
      reportMessage,
      selectedProject,
      activeProjectId,
      selectProject,
      tab,
      openProjectForm,
      closeProjectForm,
    ],
  );

  return (
    <WorkspaceNavigationProvider value={value}>
      {children}
    </WorkspaceNavigationProvider>
  );
}
