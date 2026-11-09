"use client";

import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { readError } from "../../../services/http";
import type { Agent, AgentInstance, Project, TimelineEntry, TimelinePage } from "../../../types";

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
  const [instances, setInstances] = useState<AgentInstance[]>([]);
  const [activeInstanceId, setActiveInstanceId] = useState("");
  const [timeline, setTimeline] = useState<TimelineEntry[]>([]);
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

  const refreshInstances = useCallback(async () => {
    if (!projectId || !jimAgentId) {
      setInstances([]);
      setActiveInstanceId("");
      setTimeline([]);
      return;
    }
    try {
      const response = await fetch(`/api/projects/${projectId}/agent-instances/`);
      if (!response.ok) throw new Error("Jim’s conversations could not be loaded.");
      const matchingInstances = ((await response.json()) as AgentInstance[]).filter(
        (instance) => instance.agentId === jimAgentId,
      );
      setInstances(matchingInstances);
      setActiveInstanceId((current) =>
        current && matchingInstances.some((instance) => instance.id === current)
          ? current
          : (matchingInstances[0]?.id ?? ""),
      );
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "Jim’s conversations could not be loaded.",
      );
    }
  }, [projectId, reportMessage]);

  const refreshTimeline = useCallback(async (instanceId = activeInstanceId) => {
    if (!projectId || !instanceId) {
      setTimeline([]);
      return;
    }
    try {
      const response = await fetch(
        `/api/projects/${projectId}/agent-instances/${instanceId}/timeline?limit=100`,
      );
      if (!response.ok) throw new Error("Conversation history could not be loaded.");
      setTimeline(((await response.json()) as TimelinePage).items);
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "Conversation history could not be loaded.",
      );
    }
  }, [activeInstanceId, projectId, reportMessage]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void resolveProject(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [resolveProject]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void refreshInstances(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [refreshInstances]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void refreshTimeline(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [refreshTimeline]);

  useEffect(() => {
    if (!isActive) return;
    const stream = new EventSource("/api/agent-events");
    const refresh = (event: MessageEvent<string>) => {
      const webhook = JSON.parse(event.data) as {
        projectId: string;
        data: { instanceId?: string };
      };
      if (
        webhook.projectId === projectId &&
        (!webhook.data.instanceId || webhook.data.instanceId === activeInstanceId)
      ) {
        void refreshTimeline();
        void refreshInstances();
      }
    };
    stream.addEventListener("agent.message", refresh);
    stream.addEventListener("agent.tool_call", refresh);
    return () => stream.close();
  }, [activeInstanceId, isActive, projectId, refreshInstances, refreshTimeline]);

  const start = useCallback(async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!projectId || !jimAgentId) return;
    setIsSending(true);
    reportMessage(null);
    try {
      const response = await fetch(`/api/projects/${projectId}/agents/${jimAgentId}/instances`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ initialPrompt }),
      });
      if (!response.ok) throw new Error(await readError(response));
      const instance = (await response.json()) as AgentInstance;
      setActiveInstanceId(instance.id);
      setInitialPrompt("");
      await refreshInstances();
      await refreshTimeline(instance.id);
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "The conversation could not be started.");
    } finally {
      setIsSending(false);
    }
  }, [initialPrompt, projectId, refreshInstances, refreshTimeline, reportMessage]);

  const send = useCallback(async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!projectId || !activeInstanceId || !outgoingMessage.trim()) return;
    setIsSending(true);
    reportMessage(null);
    try {
      const response = await fetch(
        `/api/projects/${projectId}/agent-instances/${activeInstanceId}/messages`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ content: outgoingMessage, delivery: messageDelivery }),
        },
      );
      if (!response.ok) throw new Error(await readError(response));
      setOutgoingMessage("");
      await refreshTimeline(activeInstanceId);
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "The message could not be sent.");
    } finally {
      setIsSending(false);
    }
  }, [activeInstanceId, messageDelivery, outgoingMessage, projectId, refreshTimeline, reportMessage]);

  const remove = useCallback(async (instance: AgentInstance) => {
    if (!projectId || !window.confirm("Delete this conversation and its execution history? This cannot be undone.")) return;
    try {
      const response = await fetch(`/api/projects/${projectId}/agent-instances/${instance.id}`, { method: "DELETE" });
      if (!response.ok) throw new Error(await readError(response));
      if (activeInstanceId === instance.id) {
        setActiveInstanceId("");
        setTimeline([]);
      }
      await refreshInstances();
    } catch (error) {
      reportMessage(error instanceof Error ? error.message : "The conversation could not be deleted.");
    }
  }, [activeInstanceId, projectId, refreshInstances, reportMessage]);

  return {
    agentId: jimAgentId, projectId, instances, activeInstanceId, timeline,
    initialPrompt, outgoingMessage, messageDelivery, isSending,
    setActiveInstanceId, setInitialPrompt, setOutgoingMessage, setMessageDelivery,
    start, send, remove,
  };
}
