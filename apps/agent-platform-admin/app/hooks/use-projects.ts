"use client";

import { useCallback, useEffect, useState } from "react";
import { readError } from "../services/http";
import type { Project } from "../types";

export function useProjects(reportError: (message: string) => void) {
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await fetch("/api/projects/");
      if (!response.ok) throw new Error("Projects could not be loaded.");
      setProjects((await response.json()) as Project[]);
    } catch (error) {
      reportError(
        error instanceof Error ? error.message : "Projects could not be loaded.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [reportError]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void refresh(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [refresh]);

  const create = useCallback(async (name: string) => {
    const response = await fetch("/api/projects/", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });

    if (!response.ok) throw new Error(await readError(response));

    const project = (await response.json()) as Project;
    setProjects((current) =>
      [...current, project].sort((a, b) => a.name.localeCompare(b.name)),
    );
    return project;
  }, []);

  return { projects, isLoading, create, refresh };
}
