"use client";

import { useCallback, useEffect, useState } from "react";
import type { Agent, AgentInstance, McpServer } from "../../../types";

export function useProjectResources(
  projectId: string,
  reportMessage: (message: string) => void,
) {
  const [agents, setAgents] = useState<Agent[]>([]);
  const [mcpServers, setMcpServers] = useState<McpServer[]>([]);
  const [agentInstances, setAgentInstances] = useState<AgentInstance[]>([]);

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

  const refreshAgentInstances = useCallback(async () => {
    if (!projectId) {
      setAgentInstances([]);
      return;
    }
    try {
      const response = await fetch(`/api/projects/${projectId}/agent-instances/`);
      if (!response.ok) throw new Error("Conversations could not be loaded.");
      setAgentInstances((await response.json()) as AgentInstance[]);
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "Conversations could not be loaded.",
      );
    }
  }, [projectId, reportMessage]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void refreshAgents();
      void refreshMcpServers();
      void refreshAgentInstances();
    }, 0);
    return () => window.clearTimeout(timeoutId);
  }, [refreshAgents, refreshAgentInstances, refreshMcpServers]);

  return {
    agents,
    mcpServers,
    agentInstances,
    refreshAgents,
    refreshMcpServers,
    refreshAgentInstances,
  };
}
