"use client";

import { useEffect, useState } from "react";
import { defaultWebhookEvents } from "../../lib/admin-page-constants";
import { readError } from "../../lib/http";
import type { Webhook, WebhookDelivery, WebhookMetrics } from "../../types";

const emptyMetrics: WebhookMetrics = { sent: 0, failed: 0, pending: 0, attempts: 0 };

export function useProjectWebhooks(projectId: string, projectName: string | undefined, reportMessage: (message: string) => void) {
  const [webhooks, setWebhooks] = useState<Webhook[]>([]);
  const [metrics, setMetrics] = useState<WebhookMetrics>(emptyMetrics);
  const [deliveries, setDeliveries] = useState<WebhookDelivery[]>([]);
  const [editing, setEditing] = useState<Webhook | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [isRetrying, setIsRetrying] = useState(false);
  const [name, setName] = useState("");
  const [url, setUrl] = useState("");
  const [secret, setSecret] = useState("");
  const [events, setEvents] = useState<string[]>(defaultWebhookEvents);

  const refresh = async () => {
    if (!projectId) { setWebhooks([]); setDeliveries([]); setMetrics(emptyMetrics); return; }
    try {
      const response = await fetch(`/api/projects/${projectId}/webhooks/`);
      if (!response.ok) throw new Error(await readError(response));
      setWebhooks((await response.json()) as Webhook[]);
      const [metricsResult, deliveriesResult] = await Promise.allSettled([fetch(`/api/projects/${projectId}/webhooks/metrics`), fetch(`/api/projects/${projectId}/webhooks/deliveries`)]);
      if (metricsResult.status === "fulfilled" && metricsResult.value.ok) setMetrics((await metricsResult.value.json()) as WebhookMetrics); else reportMessage("Webhook list loaded, but delivery metrics could not be refreshed.");
      if (deliveriesResult.status === "fulfilled" && deliveriesResult.value.ok) setDeliveries((await deliveriesResult.value.json()) as WebhookDelivery[]); else reportMessage("Webhook list loaded, but delivery messages could not be refreshed.");
    } catch (error) { reportMessage(error instanceof Error ? `Webhook list: ${error.message}` : "Webhook list could not be loaded."); }
  };

  useEffect(() => { void refresh(); }, [projectId]);
  useEffect(() => {
    const stream = new EventSource("/api/agent-events");
    const refreshForProject = (event: MessageEvent<string>) => { const message = JSON.parse(event.data) as { projectId: string }; if (message.projectId === projectId) void refresh(); };
    stream.addEventListener("agent.message", refreshForProject);
    stream.addEventListener("agent.tool_call", refreshForProject);
    return () => stream.close();
  }, [projectId]);

  const openForm = (webhook?: Webhook) => { setEditing(webhook ?? null); setName(webhook?.name ?? ""); setUrl(webhook?.endpointUrl ?? ""); setSecret(""); setEvents(webhook?.events ?? defaultWebhookEvents); setIsFormOpen(true); };
  const save = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!projectId) return; setIsSaving(true);
    try { const response = await fetch(editing ? `/api/projects/${projectId}/webhooks/${editing.id}` : `/api/projects/${projectId}/webhooks/`, { method: editing ? "PUT" : "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name, endpointUrl: url, secret: secret || undefined, events, enabled: editing?.isEnabled ?? true }) }); if (!response.ok) throw new Error(await readError(response)); setIsFormOpen(false); reportMessage(`${((await response.json()) as Webhook).name} saved.`); await refresh(); } catch (error) { reportMessage(error instanceof Error ? error.message : "The webhook could not be saved."); } finally { setIsSaving(false); }
  };
  const remove = async (webhook: Webhook) => { if (!projectId || !window.confirm(`Disable “${webhook.name}” and cancel its pending deliveries?`)) return; const response = await fetch(`/api/projects/${projectId}/webhooks/${webhook.id}`, { method: "DELETE" }); if (!response.ok) { reportMessage("The webhook could not be disabled."); return; } await refresh(); };
  const toggle = async (webhook: Webhook) => { const response = await fetch(`/api/projects/${projectId}/webhooks/${webhook.id}`, { method: "PUT", headers: { "Content-Type": "application/json" }, body: JSON.stringify({ name: webhook.name, endpointUrl: webhook.endpointUrl, events: webhook.events, enabled: !webhook.isEnabled }) }); if (!response.ok) { reportMessage(await readError(response)); return; } await refresh(); };
  const retryFailed = async () => { if (!projectId || metrics.failed === 0 || isRetrying || !window.confirm(`Resend all ${metrics.failed} failed webhook messages for ${projectName ?? "this project"}?`)) return; setIsRetrying(true); try { const response = await fetch(`/api/projects/${projectId}/webhooks/deliveries/retry-failed`, { method: "POST" }); if (!response.ok) throw new Error(await readError(response)); const { retried } = await response.json() as { retried: number }; reportMessage(`${retried} failed webhook message${retried === 1 ? "" : "s"} queued for resend.`); await refresh(); } catch (error) { reportMessage(error instanceof Error ? error.message : "Failed webhook messages could not be resent."); } finally { setIsRetrying(false); } };
  return { webhooks, metrics, deliveries, editing, isFormOpen, isSaving, isRetrying, name, url, secret, events, setName, setUrl, setSecret, setEvents, openForm, closeForm: () => setIsFormOpen(false), save, remove, toggle, refresh, retryFailed };
}
