"use client";

import { useCallback, useEffect, useState } from "react";
import type { FormEvent } from "react";
import { readError } from "../../../services/http";
import type { LlmConfiguration } from "../../../types";

export function useLlmConfigurations(reportMessage: (message: string | null) => void) {
  const [configurations, setConfigurations] = useState<LlmConfiguration[]>([]);
  const [editing, setEditing] = useState<LlmConfiguration | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [name, setName] = useState("");
  const [providerUrl, setProviderUrl] = useState("");
  const [modelName, setModelName] = useState("");
  const [apiKey, setApiKey] = useState("");

  const refresh = useCallback(async () => {
    try {
      const response = await fetch("/api/llm-configurations/");
      if (!response.ok) throw new Error("LLM configurations could not be loaded.");
      setConfigurations((await response.json()) as LlmConfiguration[]);
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "LLM configurations could not be loaded.",
      );
    }
  }, [reportMessage]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => void refresh(), 0);
    return () => window.clearTimeout(timeoutId);
  }, [refresh]);

  const openForm = useCallback((item?: LlmConfiguration) => {
    setEditing(item ?? null);
    setName(item?.name ?? "");
    setProviderUrl(item?.providerUrl ?? "");
    setModelName(item?.modelName ?? "");
    setApiKey("");
    setIsFormOpen(true);
  }, []);

  const save = useCallback(async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    try {
      const response = await fetch(
        editing ? `/api/llm-configurations/${editing.id}` : "/api/llm-configurations/",
        {
          method: editing ? "PUT" : "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name,
            providerUrl,
            modelName,
            apiKey: apiKey || undefined,
          }),
        },
      );
      if (!response.ok) throw new Error(await readError(response));
      setIsFormOpen(false);
      await refresh();
    } catch (error) {
      reportMessage(
        error instanceof Error ? error.message : "The LLM configuration could not be saved.",
      );
    } finally {
      setIsSaving(false);
    }
  }, [apiKey, editing, modelName, name, providerUrl, refresh, reportMessage]);

  const remove = useCallback(async (item: LlmConfiguration) => {
    if (!window.confirm(`Delete “${item.name}”? This cannot be undone.`)) return;
    const response = await fetch(`/api/llm-configurations/${item.id}`, { method: "DELETE" });
    if (!response.ok) {
      reportMessage("The LLM configuration could not be deleted.");
      return;
    }
    await refresh();
  }, [refresh, reportMessage]);

  return {
    configurations, editing, isFormOpen, isSaving, name, providerUrl, modelName, apiKey,
    setName, setProviderUrl, setModelName, setApiKey,
    openForm, closeForm: () => setIsFormOpen(false), save, remove,
  };
}
