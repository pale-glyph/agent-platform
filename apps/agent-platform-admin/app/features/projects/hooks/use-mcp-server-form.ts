"use client";

import { useCallback, useState } from "react";
import type { FormEvent } from "react";
import { readError } from "../../../services/http";
import type { McpServer, McpTool } from "../../../types";

export function useMcpServerForm({
  projectId,
  onSaved,
  reportMessage,
}: {
  projectId: string;
  onSaved: () => Promise<void>;
  reportMessage: (message: string | null) => void;
}) {
  const [editing, setEditing] = useState<McpServer | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [name, setName] = useState("");
  const [endpoint, setEndpoint] = useState("");
  const [cacheTtlSeconds, setCacheTtlSeconds] = useState("300");
  const [security, setSecurity] = useState<"None" | "ApiKey">("None");
  const [apiKey, setApiKey] = useState("");
  const [viewingToolsFor, setViewingToolsFor] = useState<McpServer | null>(null);
  const [tools, setTools] = useState<McpTool[]>([]);
  const [isLoadingTools, setIsLoadingTools] = useState(false);
  const [toolsError, setToolsError] = useState<string | null>(null);

  const openForm = useCallback((server?: McpServer) => {
    setEditing(server ?? null);
    setName(server?.name ?? "");
    setEndpoint(server?.endpoint ?? "");
    setCacheTtlSeconds(String(server?.cacheTtlSeconds ?? 300));
    setSecurity(server?.security ?? "None");
    setApiKey("");
    setIsFormOpen(true);
    reportMessage(null);
  }, [reportMessage]);

  const save = useCallback(async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!projectId) return;
    setIsSaving(true);
    reportMessage(null);
    try {
      const response = await fetch(
        editing
          ? `/api/projects/${projectId}/mcp-servers/${editing.id}`
          : `/api/projects/${projectId}/mcp-servers/`,
        {
          method: editing ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name,
            endpoint,
            cacheTtlSeconds: Number(cacheTtlSeconds),
            security,
            apiKey: apiKey || undefined,
          }),
        },
      );
      if (!response.ok) throw new Error(await readError(response));
      setIsFormOpen(false);
      await onSaved();
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "The MCP server could not be saved.");
    } finally {
      setIsSaving(false);
    }
  }, [apiKey, cacheTtlSeconds, editing, endpoint, name, onSaved, projectId, reportMessage, security]);

  const remove = useCallback(async (server: McpServer) => {
    if (!projectId || !window.confirm(`Delete “${server.name}”? This cannot be undone.`)) return;
    const response = await fetch(`/api/projects/${projectId}/mcp-servers/${server.id}`, { method: "DELETE" });
    if (!response.ok) {
      reportMessage("The MCP server could not be deleted.");
      return;
    }
    await onSaved();
  }, [onSaved, projectId, reportMessage]);

  const clearCache = useCallback(async (server: McpServer) => {
    if (!projectId || !window.confirm(`Clear the cached tools for “${server.name}”?`)) return;
    const response = await fetch(
      `/api/projects/${projectId}/mcp-servers/${server.id}/cache/clear`,
      { method: "POST" },
    );
    if (!response.ok) {
      reportMessage("The MCP server cache could not be cleared.");
      return;
    }
    reportMessage(`Cleared the cached tools for ${server.name}.`);
    await onSaved();
  }, [onSaved, projectId, reportMessage]);

  const viewTools = useCallback(async (server: McpServer) => {
    if (!projectId) return;
    setViewingToolsFor(server);
    setTools([]);
    setToolsError(null);
    setIsLoadingTools(true);
    try {
      const response = await fetch(`/api/projects/${projectId}/mcp-servers/${server.id}`);
      if (!response.ok) throw new Error(await readError(response));
      const result = (await response.json()) as McpTool[] | { tools?: McpTool[] };
      const discoveredTools = Array.isArray(result) ? result : result.tools;
      if (!discoveredTools || !Array.isArray(discoveredTools)) {
        throw new Error("The MCP server returned an invalid tools list.");
      }
      setTools(discoveredTools);
    } catch (error) {
      setToolsError(error instanceof Error ? error.message : "MCP tools could not be loaded.");
    } finally {
      setIsLoadingTools(false);
    }
  }, [projectId]);

  return {
    editing, isFormOpen, isSaving, name, endpoint, cacheTtlSeconds, security, apiKey,
    viewingToolsFor, tools, isLoadingTools, toolsError,
    setName, setEndpoint, setCacheTtlSeconds, setSecurity, setApiKey,
    openForm, closeForm: () => setIsFormOpen(false), save, remove, clearCache, viewTools,
    closeToolsDialog: () => setViewingToolsFor(null),
  };
}
