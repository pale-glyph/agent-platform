"use client";

import { createContext, useContext } from "react";
import type { Dispatch, FormEvent, SetStateAction } from "react";
import type { AdminPage, Project, ProjectTab } from "../types";

type WorkspaceNavigation = {
  page: AdminPage;
  setPage: Dispatch<SetStateAction<AdminPage>>;
  projects: Project[];
  selectedProject?: Project;
  selectedProjectId: string;
  isLoadingProjects: boolean;
  selectProject: (projectId: string) => void;
  openProjectForm: () => void;
  openUsage: () => void;
  tab: ProjectTab;
  setTab: Dispatch<SetStateAction<ProjectTab>>;
  message: string | null;
  reportMessage: (message: string | null) => void;
  isProjectFormOpen: boolean;
  projectName: string;
  isSavingProject: boolean;
  setProjectName: Dispatch<SetStateAction<string>>;
  createProject: (event: FormEvent<HTMLFormElement>) => Promise<void>;
  closeProjectForm: () => void;
};

const WorkspaceNavigationContext = createContext<WorkspaceNavigation | null>(
  null,
);

export const WorkspaceNavigationProvider = WorkspaceNavigationContext.Provider;

export function useWorkspaceNavigation() {
  const context = useContext(WorkspaceNavigationContext);
  if (!context) {
    throw new Error(
      "useWorkspaceNavigation must be used within WorkspaceNavigationProvider.",
    );
  }
  return context;
}
