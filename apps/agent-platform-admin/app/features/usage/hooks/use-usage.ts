"use client";

import { useCallback, useEffect, useState } from "react";
import type { Agent, LlmConfiguration, UsageBucket } from "../../../types";

export function useUsage({
  isActive,
  projectId,
  agents,
  llmConfigurations,
  reportMessage,
}: {
  isActive: boolean;
  projectId: string;
  agents: Agent[];
  llmConfigurations: LlmConfiguration[];
  reportMessage: (message: string | null) => void;
}) {
  const [platformUsage, setPlatformUsage] = useState<UsageBucket[]>([]);
  const [projectUsage, setProjectUsage] = useState<UsageBucket[]>([]);
  const [agentUsage, setAgentUsage] = useState<Record<string, UsageBucket[]>>({});
  const [llmUsage, setLlmUsage] = useState<Record<string, UsageBucket[]>>({});
  const [isLoading, setIsLoading] = useState(false);

  const refresh = useCallback(async () => {
    setIsLoading(true);
    try {
      const platformResponse = await fetch("/api/usage/platform");
      if (!platformResponse.ok) throw new Error("Platform usage could not be loaded.");
      setPlatformUsage((await platformResponse.json()) as UsageBucket[]);

      if (!projectId) {
        setProjectUsage([]);
        setAgentUsage({});
      } else {
        const projectResponse = await fetch(`/api/projects/${projectId}/usage`);
        if (!projectResponse.ok) throw new Error("Project usage could not be loaded.");
        setProjectUsage((await projectResponse.json()) as UsageBucket[]);
        const usageByAgent = await Promise.all(
          agents.map(async (agent) => {
            const response = await fetch(`/api/projects/${projectId}/agents/${agent.id}/usage`);
            return [agent.id, response.ok ? (await response.json()) as UsageBucket[] : []] as const;
          }),
        );
        setAgentUsage(Object.fromEntries(usageByAgent));
      }

      const usageByLlm = await Promise.all(
        llmConfigurations.map(async (llm) => {
          const response = await fetch(`/api/llm-configurations/${llm.id}/usage`);
          return [llm.id, response.ok ? (await response.json()) as UsageBucket[] : []] as const;
        }),
      );
      setLlmUsage(Object.fromEntries(usageByLlm));
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "Usage could not be loaded.");
    } finally {
      setIsLoading(false);
    }
  }, [agents, llmConfigurations, projectId, reportMessage]);

  useEffect(() => {
    if (!isActive) return;
    const timeoutId = window.setTimeout(() => void refresh(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [isActive, refresh]);

  return { platformUsage, projectUsage, agentUsage, llmUsage, isLoading, refresh };
}
