"use client";

import { useCallback, useState } from "react";
import type { FormEvent } from "react";
import { readError } from "../../../services/http";
import type { Agent, McpServer, McpTool } from "../../../types";

const toolKey = (serverId: string, toolName: string) => `${serverId}:${toolName}`;

export function useAgentForm({
  projectId,
  mcpServers,
  onSaved,
  reportMessage,
}: {
  projectId: string;
  mcpServers: McpServer[];
  onSaved: () => Promise<void>;
  reportMessage: (message: string | null) => void;
}) {
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [prompt, setPrompt] = useState("");
  const [llmConfigurationId, setLlmConfigurationId] = useState("");
  const [toolsByServer, setToolsByServer] = useState<Record<string, McpTool[]>>({});
  const [selectedToolKeys, setSelectedToolKeys] = useState<string[]>([]);
  const [isLoadingTools, setIsLoadingTools] = useState(false);

  const loadTools = useCallback(async () => {
    if (!projectId) return;
    setIsLoadingTools(true);
    try {
      const entries = await Promise.all(
        mcpServers.map(async (server) => {
          const response = await fetch(
            `/api/projects/${projectId}/mcp-servers/${server.id}`,
          );
          if (!response.ok) return [server.id, []] as const;
          const result = (await response.json()) as McpTool[] | { tools?: McpTool[] };
          return [server.id, Array.isArray(result) ? result : (result.tools ?? [])] as const;
        }),
      );
      setToolsByServer(Object.fromEntries(entries));
    } catch {
      reportMessage("MCP tools could not be loaded for this agent.");
    } finally {
      setIsLoadingTools(false);
    }
  }, [mcpServers, projectId, reportMessage]);

  const openForm = useCallback(
    (agent?: Agent) => {
      setEditingAgent(agent ?? null);
      setName(agent?.name ?? "");
      setDescription(agent?.description ?? "");
      setPrompt(agent?.systemPrompt ?? "");
      setLlmConfigurationId(agent?.llmConfigurationId ?? "");
      setSelectedToolKeys(
        agent?.mcpToolGrants.map((grant) => toolKey(grant.mcpServerId, grant.toolName)) ?? [],
      );
      setToolsByServer({});
      setIsFormOpen(true);
      reportMessage(null);
      void loadTools();
    },
    [loadTools, reportMessage],
  );

  const save = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!projectId) return;
      setIsSaving(true);
      reportMessage(null);
      try {
        const mcpToolGrants = selectedToolKeys.map((key) => {
          const separator = key.indexOf(":");
          return { mcpServerId: key.slice(0, separator), toolName: key.slice(separator + 1) };
        });
        const response = await fetch(
          editingAgent
            ? `/api/projects/${projectId}/agents/${editingAgent.id}`
            : `/api/projects/${projectId}/agents/`,
          {
            method: editingAgent ? "PUT" : "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              name,
              description,
              systemPrompt: prompt,
              llmConfigurationId: llmConfigurationId || null,
              mcpToolGrants,
            }),
          },
        );
        if (!response.ok) throw new Error(await readError(response));
        setIsFormOpen(false);
        await onSaved();
      } catch (error) {
        reportMessage(error instanceof Error ? error.message : "The agent could not be saved.");
      } finally {
        setIsSaving(false);
      }
    },
    [description, editingAgent, llmConfigurationId, name, onSaved, projectId, prompt, reportMessage, selectedToolKeys],
  );

  const remove = useCallback(
    async (agent: Agent) => {
      if (!projectId || !window.confirm(`Delete “${agent.name}”? This cannot be undone.`)) return;
      const response = await fetch(`/api/projects/${projectId}/agents/${agent.id}`, { method: "DELETE" });
      if (!response.ok) {
        reportMessage("The agent could not be deleted.");
        return;
      }
      await onSaved();
    },
    [onSaved, projectId, reportMessage],
  );

  return {
    editingAgent, isFormOpen, isSaving, name, description, prompt, llmConfigurationId,
    toolsByServer, selectedToolKeys, isLoadingTools,
    setName, setDescription, setPrompt, setLlmConfigurationId, setSelectedToolKeys,
    openForm, closeForm: () => setIsFormOpen(false), save, remove,
  };
}
