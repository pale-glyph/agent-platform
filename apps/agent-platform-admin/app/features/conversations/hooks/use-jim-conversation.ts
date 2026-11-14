"use client";

import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { readError } from "../../../services/http";
import type { Agent, AgentThread, Project, ThreadEntry, ThreadPage } from "../../../types";

const jimAgentId = process.env.NEXT_PUBLIC_JIM_AGENT_ID ?? "";

export function useJimConversation({
  projects,
  isActive,
  reportMessage,
}: {
  projects: Project[];
  isActive: boolean;
  reportMessage: (message: string | null) => void;
}) {
  const [projectId, setProjectId] = useState("");
  const [threads, setThreads] = useState<AgentThread[]>([]);
  const [activeThreadId, setActiveThreadId] = useState("");
  const [entries, setEntries] = useState<ThreadEntry[]>([]);
  const [initialPrompt, setInitialPrompt] = useState("");
  const [outgoingMessage, setOutgoingMessage] = useState("");
  const [messageDelivery, setMessageDelivery] = useState<"Queued" | "Steering">("Queued");
  const [isSending, setIsSending] = useState(false);

  const resolveProject = useCallback(async () => {
    if (!jimAgentId) {
      setProjectId("");
      return;
    }
    for (const project of projects) {
      const response = await fetch(`/api/projects/${project.id}/agents/`);
      if (!response.ok) continue;
      const agents = (await response.json()) as Agent[];
      if (agents.some((agent) => agent.id === jimAgentId)) {
        setProjectId(project.id);
        return;
      }
    }
    setProjectId("");
  }, [projects]);

  const refreshThreads = useCallback(async () => {
    if (!projectId || !jimAgentId) {
      setThreads([]);
      setActiveThreadId("");
      setEntries([]);
      return;
    }
    try {
      const response = await fetch(`/api/projects/${projectId}/agent-threads/`);
      if (!response.ok) throw new Error("Jim’s conversations could not be loaded.");
      const matchingThreads = ((await response.json()) as AgentThread[]).filter(
        (thread) => thread.agentId === jimAgentId,
      );
      setThreads(matchingThreads);
      setActiveThreadId((current) =>
        current && matchingThreads.some((thread) => thread.id === current)
          ? current
          : (matchingThreads[0]?.id ?? ""),
      );
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "Jim’s conversations could not be loaded.",
      );
    }
  }, [projectId, reportMessage]);

  const refreshEntries = useCallback(async (threadId = activeThreadId) => {
    if (!projectId || !threadId) {
      setEntries([]);
      return;
    }
    try {
      const response = await fetch(
        `/api/projects/${projectId}/agent-threads/${threadId}/timeline?limit=100`,
      );
      if (!response.ok) throw new Error("Conversation history could not be loaded.");
      setEntries(((await response.json()) as ThreadPage).items);
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "Conversation history could not be loaded.",
      );
    }
  }, [activeThreadId, projectId, reportMessage]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void resolveProject(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [resolveProject]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void refreshThreads(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [refreshThreads]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void refreshEntries(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [refreshEntries]);

  useEffect(() => {
    if (!isActive) return;
    const stream = new EventSource("/api/agent-events");
    const refresh = (event: MessageEvent<string>) => {
      const webhook = JSON.parse(event.data) as {
        projectId: string;
        data: { threadId?: string };
      };
      if (
        webhook.projectId === projectId &&
        (!webhook.data.threadId || webhook.data.threadId === activeThreadId)
      ) {
        void refreshEntries();
        void refreshThreads();
      }
    };
    stream.addEventListener("agent.message", refresh);
    stream.addEventListener("agent.tool_call", refresh);
    return () => stream.close();
  }, [activeThreadId, isActive, projectId, refreshEntries, refreshThreads]);

  const start = useCallback(async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!projectId || !jimAgentId) return;
    setIsSending(true);
    reportMessage(null);
    try {
      const response = await fetch(`/api/projects/${projectId}/agents/${jimAgentId}/threads`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ initialPrompt }),
      });
      if (!response.ok) throw new Error(await readError(response));
      const thread = (await response.json()) as AgentThread;
      setActiveThreadId(thread.id);
      setInitialPrompt("");
      await refreshThreads();
      await refreshEntries(thread.id);
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "The conversation could not be started.");
    } finally {
      setIsSending(false);
    }
  }, [initialPrompt, projectId, refreshEntries, refreshThreads, reportMessage]);

  const send = useCallback(async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!projectId || !activeThreadId || !outgoingMessage.trim()) return;
    setIsSending(true);
    reportMessage(null);
    try {
      const response = await fetch(
        `/api/projects/${projectId}/agent-threads/${activeThreadId}/messages`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ content: outgoingMessage, delivery: messageDelivery }),
        },
      );
      if (!response.ok) throw new Error(await readError(response));
      setOutgoingMessage("");
      await refreshEntries(activeThreadId);
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "The message could not be sent.");
    } finally {
      setIsSending(false);
    }
  }, [activeThreadId, messageDelivery, outgoingMessage, projectId, refreshEntries, reportMessage]);

  const remove = useCallback(async (thread: AgentThread) => {
    if (!projectId || !window.confirm("Delete this conversation and its execution history? This cannot be undone.")) return;
    try {
      const response = await fetch(`/api/projects/${projectId}/agent-threads/${thread.id}`, { method: "DELETE" });
      if (!response.ok) throw new Error(await readError(response));
      if (activeThreadId === thread.id) {
        setActiveThreadId("");
        setEntries([]);
      }
      await refreshThreads();
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "The conversation could not be deleted.");
    }
  }, [activeThreadId, projectId, refreshThreads, reportMessage]);

  return {
    agentId: jimAgentId, projectId, threads, activeThreadId, entries,
    initialPrompt, outgoingMessage, messageDelivery, isSending,
    setActiveThreadId, setInitialPrompt, setOutgoingMessage, setMessageDelivery,
    start, send, remove,
  };
}
