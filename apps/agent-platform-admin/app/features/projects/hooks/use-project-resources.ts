"use client";

import { useCallback, useEffect, useState } from "react";
import type { Agent, AgentThread, McpServer } from "../../../types";

export function useProjectResources(
  projectId: string,
  reportMessage: (message: string) => void,
) {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [mcpServers, setMcpServers] = useState<McpServer[]>([]);
  const [agentThreads, setAgentThreads] = useState<AgentThread[]>([]);

  const refreshAgents = useCallback(async () => {
    if (!projectId) {
      setAgents([]);
      return;
    }
    try {
      const response = await fetch(`/api/projects/${projectId}/agents/`);
      if (!response.ok) throw new Error("Agents could not be loaded.");
      setAgents((await response.json()) as Agent[]);
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "Agents could not be loaded.");
    }
  }, [projectId, reportMessage]);

  const refreshMcpServers = useCallback(async () => {
    if (!projectId) {
      setMcpServers([]);
      return;
    }
    try {
      const response = await fetch(`/api/projects/${projectId}/mcp-servers/`);
      if (!response.ok) throw new Error("MCP servers could not be loaded.");
      setMcpServers((await response.json()) as McpServer[]);
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "MCP servers could not be loaded.",
      );
    }
  }, [projectId, reportMessage]);

  const refreshAgentThreads = useCallback(async () => {
    if (!projectId) {
      setAgentThreads([]);
      return;
    }
    try {
      const response = await fetch(`/api/projects/${projectId}/agent-threads/`);
      if (!response.ok) throw new Error("Threads could not be loaded.");
      setAgentThreads((await response.json()) as AgentThread[]);
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "Threads could not be loaded.",
      );
    }
  }, [projectId, reportMessage]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void refreshAgents();
      void refreshMcpServers();
      void refreshAgentThreads();
    }, 0);
    return () => window.clearTimeout(timeoutId);
  }, [refreshAgents, refreshAgentThreads, refreshMcpServers]);

  return {
    agents,
    mcpServers,
    agentThreads,
    refreshAgents,
    refreshMcpServers,
    refreshAgentThreads,
  };
}
