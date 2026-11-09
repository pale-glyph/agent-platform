"use client";

import { useEffect, useState } from "react";
import { AppHeader } from "./components/app-header";
import { AppSidebar } from "./components/app-sidebar";
import { ProjectCreateForm } from "./components/project-create-form";
import { ConversationPage } from "./components/pages/conversation-page";
import { LlmConfigurationsPage } from "./components/pages/llm-configurations-page";
import { ProjectWorkspacePage } from "./components/pages/project-workspace-page";
import { AgentInstancesPanel } from "./components/agent-instances-panel";
import { McpToolsDialog } from "./components/mcp-tools-dialog";
import { ProjectOverview } from "./components/project-overview";
import { UsagePage } from "./components/pages/usage-page";
import {
  defaultWebhookEvents,
  projectTabs,
  webhookEventTypes,
} from "./lib/admin-page-constants";
import { readError } from "./lib/http";
import type {
  AdminPage,
  Agent,
  AgentInstance,
  LlmConfiguration,
  McpServer,
  McpTool,
  Project,
  ProjectTab,
  TimelineEntry,
  TimelinePage,
  UsageBucket,
  Webhook,
  WebhookDelivery,
  WebhookMetrics,
} from "./types";

const jimAgentId = process.env.NEXT_PUBLIC_JIM_AGENT_ID ?? "";

export default function HomePage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [tab, setTab] = useState<ProjectTab>("overview");
  const [agents, setAgents] = useState<Agent[]>([]);
  const [mcpServers, setMcpServers] = useState<McpServer[]>([]);
  const [webhooks, setWebhooks] = useState<Webhook[]>([]);
  const [webhookMetrics, setWebhookMetrics] = useState<WebhookMetrics>({
    sent: 0,
    failed: 0,
    pending: 0,
    attempts: 0,
  });
  const [webhookDeliveries, setWebhookDeliveries] = useState<WebhookDelivery[]>(
    [],
  );
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [isProjectFormOpen, setIsProjectFormOpen] = useState(false);
  const [projectName, setProjectName] = useState("");
  const [editingAgent, setEditingAgent] = useState<Agent | null>(null);
  const [isAgentFormOpen, setIsAgentFormOpen] = useState(false);
  const [agentName, setAgentName] = useState("");
  const [agentDescription, setAgentDescription] = useState("");
  const [agentPrompt, setAgentPrompt] = useState("");
  const [agentLlmConfigurationId, setAgentLlmConfigurationId] = useState("");
  const [agentMcpTools, setAgentMcpTools] = useState<Record<string, McpTool[]>>(
    {},
  );
  const [selectedMcpToolKeys, setSelectedMcpToolKeys] = useState<string[]>([]);
  const [isLoadingAgentMcpTools, setIsLoadingAgentMcpTools] = useState(false);
  const [editingMcpServer, setEditingMcpServer] = useState<McpServer | null>(
    null,
  );
  const [isMcpServerFormOpen, setIsMcpServerFormOpen] = useState(false);
  const [mcpServerName, setMcpServerName] = useState("");
  const [mcpEndpoint, setMcpEndpoint] = useState("");
  const [mcpCacheTtlSeconds, setMcpCacheTtlSeconds] = useState("300");
  const [mcpSecurity, setMcpSecurity] = useState<"None" | "ApiKey">("None");
  const [mcpApiKey, setMcpApiKey] = useState("");
  const [viewingToolsFor, setViewingToolsFor] = useState<McpServer | null>(
    null,
  );
  const [mcpTools, setMcpTools] = useState<McpTool[]>([]);
  const [isLoadingMcpTools, setIsLoadingMcpTools] = useState(false);
  const [mcpToolsError, setMcpToolsError] = useState<string | null>(null);
  const [editingWebhook, setEditingWebhook] = useState<Webhook | null>(null);
  const [isWebhookFormOpen, setIsWebhookFormOpen] = useState(false);
  const [webhookName, setWebhookName] = useState("");
  const [webhookUrl, setWebhookUrl] = useState("");
  const [webhookSecret, setWebhookSecret] = useState("");
  const [webhookEvents, setWebhookEvents] = useState<string[]>(
    defaultWebhookEvents,
  );
  const [isRetryingWebhooks, setIsRetryingWebhooks] = useState(false);
  const [llmConfigurations, setLlmConfigurations] = useState<
    LlmConfiguration[]
  >([]);
  const [isLlmFormOpen, setIsLlmFormOpen] = useState(false);
  const [editingLlm, setEditingLlm] = useState<LlmConfiguration | null>(null);
  const [llmName, setLlmName] = useState("");
  const [providerUrl, setProviderUrl] = useState("");
  const [modelName, setModelName] = useState("");
  const [apiKey, setApiKey] = useState("");
  const [page, setPage] = useState<AdminPage>("projects");
  const [platformUsage, setPlatformUsage] = useState<UsageBucket[]>([]);
  const [projectUsage, setProjectUsage] = useState<UsageBucket[]>([]);
  const [agentUsage, setAgentUsage] = useState<Record<string, UsageBucket[]>>({});
  const [llmUsage, setLlmUsage] = useState<Record<string, UsageBucket[]>>({});
  const [isLoadingUsage, setIsLoadingUsage] = useState(false);
  const [agentInstances, setAgentInstances] = useState<AgentInstance[]>([]);
  const [jimInstances, setJimInstances] = useState<AgentInstance[]>([]);
  const [activeInstanceId, setActiveInstanceId] = useState("");
  const [jimProjectId, setJimProjectId] = useState("");
  const [initialPrompt, setInitialPrompt] = useState("");
  const [timeline, setTimeline] = useState<TimelineEntry[]>([]);
  const [outgoingMessage, setOutgoingMessage] = useState("");
  const [messageDelivery, setMessageDelivery] = useState<"Queued" | "Steering">(
    "Queued",
  );
  const [isSendingMessage, setIsSendingMessage] = useState(false);

  const selectedProject = projects.find(
    (project) => project.id === selectedProjectId,
  );

  const loadProjects = async () => {
    setIsLoading(true);
    try {
      const response = await fetch("/api/projects/");
      if (!response.ok) throw new Error("Projects could not be loaded.");
      const items = (await response.json()) as Project[];
      setProjects(items);
      setSelectedProjectId((current) => current || items[0]?.id || "");
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "Projects could not be loaded.",
      );
    } finally {
      setIsLoading(false);
    }
  };

  const loadAgents = async () => {
    if (!selectedProjectId) {
      setAgents([]);
      return;
    }
    try {
      const response = await fetch(
        `/api/projects/${selectedProjectId}/agents/`,
      );
      if (!response.ok) throw new Error("Agents could not be loaded.");
      setAgents((await response.json()) as Agent[]);
    } catch (error) {
      setMessage(
        error instanceof Error ? error.message : "Agents could not be loaded.",
      );
    }
  };
  const loadMcpServers = async () => {
    if (!selectedProjectId) {
      setMcpServers([]);
      return;
    }
    try {
      const response = await fetch(
        `/api/projects/${selectedProjectId}/mcp-servers/`,
      );
      if (!response.ok) throw new Error("MCP servers could not be loaded.");
      setMcpServers((await response.json()) as McpServer[]);
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "MCP servers could not be loaded.",
      );
    }
  };
  const loadWebhooks = async () => {
    if (!selectedProjectId) {
      setWebhooks([]);
      setWebhookDeliveries([]);
      setWebhookMetrics({ sent: 0, failed: 0, pending: 0, attempts: 0 });
      return;
    }
    try {
      const response = await fetch(
        `/api/projects/${selectedProjectId}/webhooks/`,
      );
      if (!response.ok) throw new Error(await readError(response));
      setWebhooks((await response.json()) as Webhook[]);
    } catch (error) {
      setMessage(
        error instanceof Error
          ? `Webhook list: ${error.message}`
          : "Webhook list could not be loaded.",
      );
      return;
    }

    const [metricsResult, deliveriesResult] = await Promise.allSettled([
      fetch(`/api/projects/${selectedProjectId}/webhooks/metrics`),
      fetch(`/api/projects/${selectedProjectId}/webhooks/deliveries`),
    ]);
    if (metricsResult.status === "fulfilled" && metricsResult.value.ok)
      setWebhookMetrics((await metricsResult.value.json()) as WebhookMetrics);
    else
      setMessage(
        "Webhook list loaded, but delivery metrics could not be refreshed.",
      );
    if (deliveriesResult.status === "fulfilled" && deliveriesResult.value.ok)
      setWebhookDeliveries(
        (await deliveriesResult.value.json()) as WebhookDelivery[],
      );
    else
      setMessage(
        "Webhook list loaded, but delivery messages could not be refreshed.",
      );
  };
  const loadAgentInstances = async () => {
    if (!selectedProjectId) {
      setAgentInstances([]);
      setActiveInstanceId("");
      return;
    }
    try {
      const response = await fetch(
        `/api/projects/${selectedProjectId}/agent-instances/`,
      );
      if (!response.ok) throw new Error("Conversations could not be loaded.");
      const items = (await response.json()) as AgentInstance[];
      setAgentInstances(items);
      setActiveInstanceId((current) =>
        current && items.some((item) => item.id === current)
          ? current
          : (items[0]?.id ?? ""),
      );
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "Conversations could not be loaded.",
      );
    }
  };
  const resolveJimProject = async () => {
    if (!jimAgentId) {
      setJimProjectId("");
      return;
    }
    for (const project of projects) {
      const response = await fetch(`/api/projects/${project.id}/agents/`);
      if (!response.ok) continue;
      const projectAgents = (await response.json()) as Agent[];
      if (projectAgents.some((agent) => agent.id === jimAgentId)) {
        setJimProjectId(project.id);
        return;
      }
    }
    setJimProjectId("");
  };
  const loadJimInstances = async () => {
    if (!jimProjectId || !jimAgentId) {
      setJimInstances([]);
      setActiveInstanceId("");
      setTimeline([]);
      return;
    }
    try {
      const response = await fetch(
        `/api/projects/${jimProjectId}/agent-instances/`,
      );
      if (!response.ok)
        throw new Error("Jim’s conversations could not be loaded.");
      const items = ((await response.json()) as AgentInstance[]).filter(
        (item) => item.agentId === jimAgentId,
      );
      setJimInstances(items);
      setActiveInstanceId((current) =>
        current && items.some((item) => item.id === current)
          ? current
          : (items[0]?.id ?? ""),
      );
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "Jim’s conversations could not be loaded.",
      );
    }
  };
  const loadTimeline = async (instanceId = activeInstanceId) => {
    if (!jimProjectId || !instanceId) {
      setTimeline([]);
      return;
    }
    try {
      const response = await fetch(
        `/api/projects/${jimProjectId}/agent-instances/${instanceId}/timeline?limit=100`,
      );
      if (!response.ok)
        throw new Error("Conversation history could not be loaded.");
      setTimeline(((await response.json()) as TimelinePage).items);
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "Conversation history could not be loaded.",
      );
    }
  };

  useEffect(() => {
    void loadProjects();
  }, []);
  useEffect(() => {
    void loadAgents();
  }, [selectedProjectId]);
  useEffect(() => {
    void loadMcpServers();
  }, [selectedProjectId]);
  useEffect(() => {
    void loadWebhooks();
  }, [selectedProjectId]);
  useEffect(() => {
    void loadAgentInstances();
  }, [selectedProjectId]);
  useEffect(() => {
    void resolveJimProject();
  }, [projects]);
  useEffect(() => {
    void loadJimInstances();
  }, [jimProjectId]);
  useEffect(() => {
    void loadTimeline();
  }, [activeInstanceId, jimProjectId]);
  useEffect(() => {
    const stream = new EventSource("/api/agent-events");
    const refresh = (event: MessageEvent<string>) => {
      const webhook = JSON.parse(event.data) as {
        projectId: string;
        data: { instanceId?: string };
      };
      if (webhook.projectId === selectedProjectId && tab === "webhooks")
        void loadWebhooks();
      if (
        page === "chat" &&
        webhook.projectId === jimProjectId &&
        (!webhook.data.instanceId || webhook.data.instanceId === activeInstanceId)
      ) {
        void loadTimeline();
        void loadJimInstances();
      }
    };
    stream.addEventListener("agent.message", refresh);
    stream.addEventListener("agent.tool_call", refresh);
    return () => stream.close();
  }, [activeInstanceId, jimProjectId, page, selectedProjectId, tab]);
  const loadLlmConfigurations = async () => {
    try {
      const response = await fetch("/api/llm-configurations/");
      if (!response.ok)
        throw new Error("LLM configurations could not be loaded.");
      setLlmConfigurations((await response.json()) as LlmConfiguration[]);
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "LLM configurations could not be loaded.",
      );
    }
  };
  useEffect(() => {
    void loadLlmConfigurations();
  }, []);

  const loadUsage = async () => {
    setIsLoadingUsage(true);
    try {
      const platformResponse = await fetch("/api/usage/platform");
      if (!platformResponse.ok) throw new Error("Platform usage could not be loaded.");
      setPlatformUsage((await platformResponse.json()) as UsageBucket[]);

      if (!selectedProjectId) {
        setProjectUsage([]);
        setAgentUsage({});
      } else {
        const projectResponse = await fetch(`/api/projects/${selectedProjectId}/usage`);
        if (!projectResponse.ok) throw new Error("Project usage could not be loaded.");
        setProjectUsage((await projectResponse.json()) as UsageBucket[]);
        const usageByAgent = await Promise.all(agents.map(async (agent) => {
          const response = await fetch(`/api/projects/${selectedProjectId}/agents/${agent.id}/usage`);
          return [agent.id, response.ok ? (await response.json()) as UsageBucket[] : []] as const;
        }));
        setAgentUsage(Object.fromEntries(usageByAgent));
      }

      const usageByLlm = await Promise.all(llmConfigurations.map(async (llm) => {
        const response = await fetch(`/api/llm-configurations/${llm.id}/usage`);
        return [llm.id, response.ok ? (await response.json()) as UsageBucket[] : []] as const;
      }));
      setLlmUsage(Object.fromEntries(usageByLlm));
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Usage could not be loaded.");
    } finally {
      setIsLoadingUsage(false);
    }
  };
  const selectProject = (projectId: string) => {
    setSelectedProjectId(projectId);
    setTab("overview");
    setMessage(null);
  };
  const createProject = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    setMessage(null);
    const response = await fetch("/api/projects/", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: projectName }),
    });
    if (!response.ok) {
      setMessage(await readError(response));
      setIsSaving(false);
      return;
    }
    const project = (await response.json()) as Project;
    setProjects((items) =>
      [...items, project].sort((a, b) => a.name.localeCompare(b.name)),
    );
    selectProject(project.id);
    setProjectName("");
    setIsProjectFormOpen(false);
    setIsSaving(false);
  };

  const toolKey = (serverId: string, toolName: string) =>
    `${serverId}:${toolName}`;
  const loadAgentMcpTools = async () => {
    if (!selectedProjectId) return;
    setIsLoadingAgentMcpTools(true);
    try {
      const entries = await Promise.all(
        mcpServers.map(async (server) => {
          const response = await fetch(
            `/api/projects/${selectedProjectId}/mcp-servers/${server.id}`,
          );
          if (!response.ok) return [server.id, []] as const;
          const result = (await response.json()) as
            | { tools?: McpTool[] }
            | McpTool[];
          return [
            server.id,
            Array.isArray(result) ? result : (result.tools ?? []),
          ] as const;
        }),
      );
      setAgentMcpTools(Object.fromEntries(entries));
    } catch {
      setMessage("MCP tools could not be loaded for this agent.");
    } finally {
      setIsLoadingAgentMcpTools(false);
    }
  };
  const openAgentForm = (agent?: Agent) => {
    setEditingAgent(agent ?? null);
    setAgentName(agent?.name ?? "");
    setAgentDescription(agent?.description ?? "");
    setAgentPrompt(agent?.systemPrompt ?? "");
    setAgentLlmConfigurationId(agent?.llmConfigurationId ?? "");
    setSelectedMcpToolKeys(
      agent?.mcpToolGrants.map((grant) =>
        toolKey(grant.mcpServerId, grant.toolName),
      ) ?? [],
    );
    setAgentMcpTools({});
    setIsAgentFormOpen(true);
    setMessage(null);
    void loadAgentMcpTools();
  };

  const saveAgent = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedProjectId) return;
    setIsSaving(true);
    setMessage(null);
    const mcpToolGrants = selectedMcpToolKeys.map((key) => {
      const separator = key.indexOf(":");
      return {
        mcpServerId: key.slice(0, separator),
        toolName: key.slice(separator + 1),
      };
    });
    const response = await fetch(
      editingAgent
        ? `/api/projects/${selectedProjectId}/agents/${editingAgent.id}`
        : `/api/projects/${selectedProjectId}/agents/`,
      {
        method: editingAgent ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: agentName,
          description: agentDescription,
          systemPrompt: agentPrompt,
          llmConfigurationId: agentLlmConfigurationId || null,
          mcpToolGrants,
        }),
      },
    );
    if (!response.ok) {
      setMessage(await readError(response));
      setIsSaving(false);
      return;
    }
    setIsAgentFormOpen(false);
    setIsSaving(false);
    await loadAgents();
  };

  const deleteAgent = async (agent: Agent) => {
    if (
      !selectedProjectId ||
      !window.confirm(`Delete “${agent.name}”? This cannot be undone.`)
    )
      return;
    const response = await fetch(
      `/api/projects/${selectedProjectId}/agents/${agent.id}`,
      { method: "DELETE" },
    );
    if (!response.ok) {
      setMessage("The agent could not be deleted.");
      return;
    }
    await loadAgents();
  };
  const startConversation = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!jimProjectId || !jimAgentId) return;
    setIsSendingMessage(true);
    setMessage(null);
    try {
      const response = await fetch(
        `/api/projects/${jimProjectId}/agents/${jimAgentId}/instances`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ initialPrompt }),
        },
      );
      if (!response.ok) throw new Error(await readError(response));
      const instance = (await response.json()) as AgentInstance;
      setActiveInstanceId(instance.id);
      setInitialPrompt("");
      await loadJimInstances();
      await loadTimeline(instance.id);
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "The conversation could not be started.",
      );
    } finally {
      setIsSendingMessage(false);
    }
  };
  const sendConversationMessage = async (
    event: React.FormEvent<HTMLFormElement>,
  ) => {
    event.preventDefault();
    if (!jimProjectId || !activeInstanceId || !outgoingMessage.trim()) return;
    setIsSendingMessage(true);
    setMessage(null);
    try {
      const response = await fetch(
        `/api/projects/${jimProjectId}/agent-instances/${activeInstanceId}/messages`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            content: outgoingMessage,
            delivery: messageDelivery,
          }),
        },
      );
      if (!response.ok) throw new Error(await readError(response));
      setOutgoingMessage("");
      await loadTimeline(activeInstanceId);
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "The message could not be sent.",
      );
    } finally {
      setIsSendingMessage(false);
    }
  };
  const deleteConversation = async (instance: AgentInstance) => {
    if (!jimProjectId || !window.confirm("Delete this conversation and its execution history? This cannot be undone.")) return;
    try {
      const response = await fetch(`/api/projects/${jimProjectId}/agent-instances/${instance.id}`, { method: "DELETE" });
      if (!response.ok) throw new Error(await readError(response));
      if (activeInstanceId === instance.id) {
        setActiveInstanceId("");
        setTimeline([]);
      }
      await loadJimInstances();
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "The conversation could not be deleted.");
    }
  };

  const openMcpServerForm = (server?: McpServer) => {
    setEditingMcpServer(server ?? null);
    setMcpServerName(server?.name ?? "");
    setMcpEndpoint(server?.endpoint ?? "");
    setMcpCacheTtlSeconds(String(server?.cacheTtlSeconds ?? 300));
    setMcpSecurity(server?.security ?? "None");
    setMcpApiKey("");
    setIsMcpServerFormOpen(true);
    setMessage(null);
  };
  const saveMcpServer = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedProjectId) return;
    setIsSaving(true);
    setMessage(null);
    const response = await fetch(
      editingMcpServer
        ? `/api/projects/${selectedProjectId}/mcp-servers/${editingMcpServer.id}`
        : `/api/projects/${selectedProjectId}/mcp-servers/`,
      {
        method: editingMcpServer ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: mcpServerName,
          endpoint: mcpEndpoint,
          cacheTtlSeconds: Number(mcpCacheTtlSeconds),
          security: mcpSecurity,
          apiKey: mcpApiKey || undefined,
        }),
      },
    );
    if (!response.ok) {
      setMessage(await readError(response));
      setIsSaving(false);
      return;
    }
    setIsSaving(false);
    setIsMcpServerFormOpen(false);
    await loadMcpServers();
  };
  const deleteMcpServer = async (server: McpServer) => {
    if (
      !selectedProjectId ||
      !window.confirm(`Delete “${server.name}”? This cannot be undone.`)
    )
      return;
    const response = await fetch(
      `/api/projects/${selectedProjectId}/mcp-servers/${server.id}`,
      { method: "DELETE" },
    );
    if (!response.ok) {
      setMessage("The MCP server could not be deleted.");
      return;
    }
    await loadMcpServers();
  };
  const clearMcpServerCache = async (server: McpServer) => {
    if (
      !selectedProjectId ||
      !window.confirm(`Clear the cached tools for “${server.name}”?`)
    )
      return;
    const response = await fetch(
      `/api/projects/${selectedProjectId}/mcp-servers/${server.id}/cache/clear`,
      { method: "POST" },
    );
    if (!response.ok) {
      setMessage("The MCP server cache could not be cleared.");
      return;
    }
    setMessage(`Cleared the cached tools for ${server.name}.`);
    await loadMcpServers();
  };
  const viewMcpTools = async (server: McpServer) => {
    if (!selectedProjectId) return;
    setViewingToolsFor(server);
    setMcpTools([]);
    setMcpToolsError(null);
    setIsLoadingMcpTools(true);
    try {
      const response = await fetch(
        `/api/projects/${selectedProjectId}/mcp-servers/${server.id}`,
      );
      if (!response.ok) throw new Error(await readError(response));
      const result = (await response.json()) as
        | { tools?: McpTool[] }
        | McpTool[];
      const tools = Array.isArray(result) ? result : result.tools;
      if (!tools || !Array.isArray(tools))
        throw new Error("The MCP server returned an invalid tools list.");
      setMcpTools(tools);
    } catch (error) {
      setMcpToolsError(
        error instanceof Error
          ? error.message
          : "MCP tools could not be loaded.",
      );
    } finally {
      setIsLoadingMcpTools(false);
    }
  };
  const openWebhookForm = (webhook?: Webhook) => {
    setEditingWebhook(webhook ?? null);
    setWebhookName(webhook?.name ?? "");
    setWebhookUrl(webhook?.endpointUrl ?? "");
    setWebhookSecret("");
    setWebhookEvents(
      webhook?.events ?? defaultWebhookEvents,
    );
    setIsWebhookFormOpen(true);
    setMessage(null);
  };
  const saveWebhook = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!selectedProjectId) return;
    setIsSaving(true);
    setMessage(null);
    const response = await fetch(
      editingWebhook
        ? `/api/projects/${selectedProjectId}/webhooks/${editingWebhook.id}`
        : `/api/projects/${selectedProjectId}/webhooks/`,
      {
        method: editingWebhook ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: webhookName,
          endpointUrl: webhookUrl,
          secret: webhookSecret || undefined,
          events: webhookEvents,
          enabled: editingWebhook?.isEnabled ?? true,
        }),
      },
    );
    if (!response.ok) {
      setMessage(await readError(response));
      setIsSaving(false);
      return;
    }
    const savedWebhook = (await response.json()) as Webhook;
    setWebhooks((items) =>
      editingWebhook
        ? items.map((item) =>
            item.id === savedWebhook.id ? savedWebhook : item,
          )
        : [...items, savedWebhook].sort((a, b) => a.name.localeCompare(b.name)),
    );
    setIsSaving(false);
    setIsWebhookFormOpen(false);
    setMessage(`${savedWebhook.name} saved.`);
    await loadWebhooks();
  };
  const deleteWebhook = async (webhook: Webhook) => {
    if (
      !selectedProjectId ||
      !window.confirm(
        `Disable “${webhook.name}” and cancel its pending deliveries?`,
      )
    )
      return;
    const response = await fetch(
      `/api/projects/${selectedProjectId}/webhooks/${webhook.id}`,
      { method: "DELETE" },
    );
    if (!response.ok) {
      setMessage("The webhook could not be disabled.");
      return;
    }
    await loadWebhooks();
  };
  const retryFailedWebhooks = async () => {
    if (!selectedProjectId || webhookMetrics.failed === 0 || isRetryingWebhooks)
      return;
    if (
      !window.confirm(
        `Resend all ${webhookMetrics.failed} failed webhook messages for ${selectedProject?.name ?? "this project"}?`,
      )
    )
      return;
    setIsRetryingWebhooks(true);
    try {
      const response = await fetch(
        `/api/projects/${selectedProjectId}/webhooks/deliveries/retry-failed`,
        { method: "POST" },
      );
      if (!response.ok) {
        setMessage(await readError(response));
        return;
      }
      const result = (await response.json()) as { retried: number };
      setMessage(
        `${result.retried} failed webhook message${result.retried === 1 ? "" : "s"} queued for resend.`,
      );
      await loadWebhooks();
    } catch (error) {
      setMessage(
        error instanceof Error
          ? error.message
          : "Failed webhook messages could not be resent.",
      );
    } finally {
      setIsRetryingWebhooks(false);
    }
  };
  const toggleWebhook = async (webhook: Webhook) => {
    const response = await fetch(
      `/api/projects/${selectedProjectId}/webhooks/${webhook.id}`,
      {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: webhook.name,
          endpointUrl: webhook.endpointUrl,
          events: webhook.events,
          enabled: !webhook.isEnabled,
        }),
      },
    );
    if (!response.ok) {
      setMessage(await readError(response));
      return;
    }
    await loadWebhooks();
  };

  const openLlmForm = (item?: LlmConfiguration) => {
    setEditingLlm(item ?? null);
    setLlmName(item?.name ?? "");
    setProviderUrl(item?.providerUrl ?? "");
    setModelName(item?.modelName ?? "");
    setApiKey("");
    setIsLlmFormOpen(true);
  };
  const saveLlm = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    const response = await fetch(
      editingLlm
        ? `/api/llm-configurations/${editingLlm.id}`
        : "/api/llm-configurations/",
      {
        method: editingLlm ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          name: llmName,
          providerUrl,
          modelName,
          apiKey: apiKey || undefined,
        }),
      },
    );
    if (!response.ok) {
      setMessage(await readError(response));
      setIsSaving(false);
      return;
    }
    setIsSaving(false);
    setIsLlmFormOpen(false);
    await loadLlmConfigurations();
  };
  const deleteLlm = async (item: LlmConfiguration) => {
    if (!window.confirm(`Delete “${item.name}”? This cannot be undone.`))
      return;
    const response = await fetch(`/api/llm-configurations/${item.id}`, {
      method: "DELETE",
    });
    if (!response.ok) {
      setMessage("The LLM configuration could not be deleted.");
      return;
    }
    await loadLlmConfigurations();
  };

  return (
    <div className="app-shell">
      <AppSidebar page={page} projects={projects} selectedProjectId={selectedProjectId} isLoading={isLoading} onPageChange={setPage} onUsageSelect={() => { setPage("usage"); void loadUsage(); }} onProjectSelect={selectProject} onCreateProject={() => setIsProjectFormOpen(true)} />

      <div className="workspace" id="workspace">
        <AppHeader page={page} selectedProject={selectedProject} onCreateProject={() => setIsProjectFormOpen(true)} onCreateLlm={() => openLlmForm()} />
        <main>
          {message && (
            <p className="status-message" role="alert">
              {message}
            </p>
          )}
          {page === "projects" && isProjectFormOpen && <ProjectCreateForm name={projectName} isSaving={isSaving} onNameChange={setProjectName} onSubmit={createProject} onCancel={() => setIsProjectFormOpen(false)} />}
          {page === "projects" &&
            (!selectedProject ? (
              <div className="empty-state workspace-empty">
                <strong>Select a project</strong>
                <span>
                  Choose a project from the sidebar or create a new one to
                  begin.
                </span>
              </div>
            ) : (
              <ProjectWorkspacePage project={selectedProject} tab={tab} tabs={projectTabs} onTabChange={setTab}>
                {tab === "overview" && (
                  <ProjectOverview project={selectedProject} agentCount={agents.length} serverCount={mcpServers.length} />
                )}
                {tab === "agents" && (
                  <section className="project-panel tab-panel">
                    <div className="panel-heading">
                      <div>
                        <p className="eyebrow">Agents</p>
                        <h2>Agent definitions</h2>
                      </div>
                      <button
                        className="new-button"
                        type="button"
                        onClick={() => openAgentForm()}
                      >
                        New agent
                      </button>
                    </div>
                    {isAgentFormOpen && (
                      <form className="agent-form" onSubmit={saveAgent}>
                        <label htmlFor="agent-name">Name</label>
                        <input
                          id="agent-name"
                          value={agentName}
                          maxLength={200}
                          onChange={(event) => setAgentName(event.target.value)}
                          required
                        />
                        <label htmlFor="agent-description">Description</label>
                        <textarea
                          id="agent-description"
                          value={agentDescription}
                          maxLength={2000}
                          onChange={(event) =>
                            setAgentDescription(event.target.value)
                          }
                          required
                        />
                        <label htmlFor="agent-prompt">System prompt</label>
                        <textarea
                          id="agent-prompt"
                          value={agentPrompt}
                          maxLength={20000}
                          onChange={(event) =>
                            setAgentPrompt(event.target.value)
                          }
                          required
                        />
                        <label htmlFor="agent-llm">LLM configuration</label>
                        <select
                          id="agent-llm"
                          value={agentLlmConfigurationId}
                          onChange={(event) =>
                            setAgentLlmConfigurationId(event.target.value)
                          }
                        >
                          <option value="">
                            No LLM selected — agent will be misconfigured
                          </option>
                          {llmConfigurations.map((llm) => (
                            <option key={llm.id} value={llm.id}>
                              {llm.name} ({llm.modelName})
                            </option>
                          ))}
                        </select>
                        <fieldset className="mcp-tool-options">
                          <legend>MCP tools</legend>
                          <p>
                            Select only the tools this agent may call. Access is
                            deny-by-default.
                          </p>
                          {isLoadingAgentMcpTools ? (
                            <span>Loading project tools…</span>
                          ) : mcpServers.length === 0 ? (
                            <span>
                              No MCP servers are configured for this project.
                            </span>
                          ) : (
                            mcpServers.map((server) => (
                              <div className="mcp-tool-server" key={server.id}>
                                <strong>{server.name}</strong>
                                {(agentMcpTools[server.id] ?? []).length ===
                                0 ? (
                                  <small>No tools available.</small>
                                ) : (
                                  (agentMcpTools[server.id] ?? []).map(
                                    (tool) => {
                                      const key = toolKey(server.id, tool.name);
                                      return (
                                        <label key={key}>
                                          <input
                                            type="checkbox"
                                            checked={selectedMcpToolKeys.includes(
                                              key,
                                            )}
                                            onChange={(event) =>
                                              setSelectedMcpToolKeys(
                                                (current) =>
                                                  event.target.checked
                                                    ? [...current, key]
                                                    : current.filter(
                                                        (item) => item !== key,
                                                      ),
                                              )
                                            }
                                          />{" "}
                                          <span>{tool.name}</span>
                                          {tool.description && (
                                            <small>{tool.description}</small>
                                          )}
                                        </label>
                                      );
                                    },
                                  )
                                )}
                              </div>
                            ))
                          )}
                        </fieldset>
                        <div>
                          <button className="new-button" disabled={isSaving}>
                            {isSaving
                              ? "Saving…"
                              : editingAgent
                                ? "Save changes"
                                : "Create agent"}
                          </button>
                          <button
                            className="text-button"
                            type="button"
                            onClick={() => setIsAgentFormOpen(false)}
                          >
                            Cancel
                          </button>
                        </div>
                      </form>
                    )}
                    {agents.length === 0 ? (
                      <div className="empty-state">
                        <strong>No agents yet</strong>
                        <span>
                          Create an agent definition for {selectedProject.name}.
                        </span>
                      </div>
                    ) : (
                      <ul className="project-list">
                        {agents.map((agent) => (
                          <li key={agent.id}>
                            <span className="project-badge">
                              {agent.name.slice(0, 1).toUpperCase()}
                            </span>
                            <div>
                              <strong>{agent.name}</strong>
                              {!agent.llmConfigurationId && (
                                <span className="agent-misconfigured">
                                  Misconfigured: no LLM selected
                                </span>
                              )}
                              <small>{agent.description}</small>
                              <small>
                                {agent.mcpToolGrants.length} MCP tool
                                {agent.mcpToolGrants.length === 1 ? "" : "s"}{" "}
                                granted
                              </small>
                            </div>
                            <div className="project-actions">
                              <button onClick={() => openAgentForm(agent)}>
                                Edit
                              </button>
                              <button onClick={() => void deleteAgent(agent)}>
                                Delete
                              </button>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                  </section>
                )}
                {tab === "servers" && (
                  <section className="project-panel tab-panel">
                    <div className="panel-heading">
                      <div>
                        <p className="eyebrow">MCP servers</p>
                        <h2>Project tool integrations</h2>
                      </div>
                      <button
                        className="new-button"
                        type="button"
                        onClick={() => openMcpServerForm()}
                      >
                        New server
                      </button>
                    </div>
                    {isMcpServerFormOpen && (
                      <form className="agent-form" onSubmit={saveMcpServer}>
                        <label htmlFor="mcp-server-name">Name</label>
                        <input
                          id="mcp-server-name"
                          value={mcpServerName}
                          maxLength={200}
                          onChange={(event) =>
                            setMcpServerName(event.target.value)
                          }
                          required
                        />
                        <label htmlFor="mcp-endpoint">Server endpoint</label>
                        <input
                          id="mcp-endpoint"
                          type="url"
                          value={mcpEndpoint}
                          maxLength={2000}
                          onChange={(event) =>
                            setMcpEndpoint(event.target.value)
                          }
                          required
                        />
                        <label htmlFor="mcp-cache-ttl">
                          Tool cache TTL (seconds)
                        </label>
                        <input
                          id="mcp-cache-ttl"
                          type="number"
                          min="1"
                          max="86400"
                          value={mcpCacheTtlSeconds}
                          onChange={(event) =>
                            setMcpCacheTtlSeconds(event.target.value)
                          }
                          required
                        />
                        <label htmlFor="mcp-security">Security</label>
                        <select
                          id="mcp-security"
                          value={mcpSecurity}
                          onChange={(event) =>
                            setMcpSecurity(
                              event.target.value as "None" | "ApiKey",
                            )
                          }
                        >
                          <option value="None">None</option>
                          <option value="ApiKey">API key</option>
                        </select>
                        {mcpSecurity === "ApiKey" && (
                          <>
                            <label htmlFor="mcp-api-key">
                              API key{" "}
                              {editingMcpServer && (
                                <small>(leave blank to keep current key)</small>
                              )}
                            </label>
                            <input
                              id="mcp-api-key"
                              type="password"
                              value={mcpApiKey}
                              onChange={(event) =>
                                setMcpApiKey(event.target.value)
                              }
                              required={!editingMcpServer}
                            />
                          </>
                        )}
                        <div>
                          <button className="new-button" disabled={isSaving}>
                            {isSaving
                              ? "Saving…"
                              : editingMcpServer
                                ? "Save changes"
                                : "Create server"}
                          </button>
                          <button
                            className="text-button"
                            type="button"
                            onClick={() => setIsMcpServerFormOpen(false)}
                          >
                            Cancel
                          </button>
                        </div>
                      </form>
                    )}
                    {mcpServers.length === 0 ? (
                      <div className="empty-state">
                        <strong>No MCP servers yet</strong>
                        <span>
                          Add a project-scoped MCP server to discover and cache
                          its tools.
                        </span>
                      </div>
                    ) : (
                      <ul className="project-list">
                        {mcpServers.map((server) => (
                          <li key={server.id}>
                            <span className="project-badge">⌘</span>
                            <div>
                              <strong>{server.name}</strong>
                              <small>
                                {server.security === "ApiKey"
                                  ? "API key"
                                  : "No auth"}{" "}
                                · cache {server.cacheTtlSeconds}s ·{" "}
                                {server.endpoint}
                              </small>
                            </div>
                            <div className="project-actions">
                              <button onClick={() => void viewMcpTools(server)}>
                                View tools
                              </button>
                              <button
                                onClick={() => void clearMcpServerCache(server)}
                              >
                                Clear cache
                              </button>
                              <button onClick={() => openMcpServerForm(server)}>
                                Edit
                              </button>
                              <button
                                onClick={() => void deleteMcpServer(server)}
                              >
                                Delete
                              </button>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                  </section>
                )}
                {tab === "webhooks" && (
                  <section className="project-panel tab-panel">
                    <div className="panel-heading">
                      <div>
                        <p className="eyebrow">Webhooks</p>
                        <h2>Project event delivery</h2>
                      </div>
                      <button
                        className="new-button"
                        type="button"
                        onClick={() => openWebhookForm()}
                      >
                        New webhook
                      </button>
                    </div>
                    <p className="webhook-security-note">
                      Deliveries use HTTPS, except for the internal admin service,
                      and include an HMAC-SHA256 signature
                      in <code>X-Agent-Platform-Signature</code>. Use the event
                      ID to handle retries idempotently.
                    </p>
                    {isWebhookFormOpen && (
                      <form className="agent-form" onSubmit={saveWebhook}>
                        <label htmlFor="webhook-name">Name</label>
                        <input
                          id="webhook-name"
                          value={webhookName}
                          maxLength={200}
                          onChange={(event) =>
                            setWebhookName(event.target.value)
                          }
                          required
                        />
                        <div className="webhook-url-label">
                          <label htmlFor="webhook-url">
                            HTTPS endpoint <small>(HTTP is allowed for the internal admin service)</small>
                          </label>
                          <button
                            className="text-button"
                            type="button"
                            onClick={() =>
                              setWebhookUrl(
                                "http://localhost:8080/webhooks/receive",
                              )
                            }
                          >
                            Use built-in receiver
                          </button>
                        </div>
                        <input
                          id="webhook-url"
                          type="url"
                          value={webhookUrl}
                          maxLength={2000}
                          onChange={(event) =>
                            setWebhookUrl(event.target.value)
                          }
                          placeholder="https://receiver.example/events"
                          required
                        />
                        <label htmlFor="webhook-secret">
                          Signing secret{" "}
                          {editingWebhook && (
                            <small>(leave blank to keep current secret)</small>
                          )}
                        </label>
                        <input
                          id="webhook-secret"
                          type="password"
                          minLength={32}
                          maxLength={1000}
                          autoComplete="new-password"
                          value={webhookSecret}
                          onChange={(event) =>
                            setWebhookSecret(event.target.value)
                          }
                          required={!editingWebhook}
                        />
                        <fieldset className="webhook-event-options">
                          <legend>Events to send</legend>
                          {webhookEventTypes.map(([eventType, label]) => (
                            <label key={eventType}>
                              <input
                                type="checkbox"
                                checked={webhookEvents.includes(eventType)}
                                onChange={(event) =>
                                  setWebhookEvents((current) =>
                                    event.target.checked
                                      ? [...current, eventType]
                                      : current.filter(
                                          (item) => item !== eventType,
                                        ),
                                  )
                                }
                              />
                              {label}
                            </label>
                          ))}
                        </fieldset>
                        <div>
                          <button
                            className="new-button"
                            disabled={isSaving || webhookEvents.length === 0}
                          >
                            {isSaving
                              ? "Saving…"
                              : editingWebhook
                                ? "Save changes"
                                : "Create webhook"}
                          </button>
                          <button
                            className="text-button"
                            type="button"
                            onClick={() => setIsWebhookFormOpen(false)}
                          >
                            Cancel
                          </button>
                        </div>
                      </form>
                    )}
                    <div
                      className="webhook-metrics"
                      aria-label="Webhook delivery metrics"
                    >
                      <div>
                        <span>Sent</span>
                        <strong>{webhookMetrics.sent}</strong>
                      </div>
                      <div>
                        <span>Pending</span>
                        <strong>{webhookMetrics.pending}</strong>
                      </div>
                      <div>
                        <span>Failed</span>
                        <strong>{webhookMetrics.failed}</strong>
                      </div>
                      <div>
                        <span>Attempts</span>
                        <strong>{webhookMetrics.attempts}</strong>
                      </div>
                    </div>
                    {webhooks.length === 0 ? (
                      <div className="empty-state">
                        <strong>No webhooks configured</strong>
                        <span>
                          Add a secure HTTPS receiver and select the project
                          events it should receive.
                        </span>
                      </div>
                    ) : (
                      <ul className="project-list">
                        {webhooks.map((webhook) => (
                          <li key={webhook.id}>
                            <span className="project-badge">↗</span>
                            <div className="webhook-item-copy">
                              <strong>
                                {webhook.name}
                                {!webhook.isEnabled && (
                                  <span className="webhook-disabled">
                                    Disabled
                                  </span>
                                )}
                              </strong>
                              <small>{webhook.endpointUrl}</small>
                              <small>{webhook.events.join(" · ")}</small>
                            </div>
                            <div className="project-actions">
                              <button
                                onClick={() => void toggleWebhook(webhook)}
                              >
                                {webhook.isEnabled ? "Disable" : "Enable"}
                              </button>
                              <button onClick={() => openWebhookForm(webhook)}>
                                Edit
                              </button>
                              <button
                                onClick={() => void deleteWebhook(webhook)}
                              >
                                Delete
                              </button>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                    <section
                      className="webhook-deliveries"
                      aria-labelledby="webhook-deliveries-title"
                    >
                      <div className="deliveries-heading">
                        <div>
                          <p className="eyebrow">Delivery queue</p>
                          <h3 id="webhook-deliveries-title">
                            Pending and failed messages
                          </h3>
                        </div>
                        <div className="delivery-actions">
                          <button
                            className="text-button"
                            type="button"
                            onClick={() => void loadWebhooks()}
                          >
                            Refresh
                          </button>
                          <button
                            className="text-button"
                            type="button"
                            disabled={
                              webhookMetrics.failed === 0 || isRetryingWebhooks
                            }
                            onClick={() => void retryFailedWebhooks()}
                          >
                            {isRetryingWebhooks
                              ? "Resending…"
                              : `Resend all failed (${webhookMetrics.failed})`}
                          </button>
                        </div>
                      </div>
                      {webhookDeliveries.length === 0 ? (
                        <div className="empty-state compact-empty">
                          <strong>No pending or failed messages</strong>
                          <span>
                            Delivery activity will appear here when subscribed
                            events are emitted.
                          </span>
                        </div>
                      ) : (
                        <div className="delivery-table-wrap">
                          <table className="delivery-table">
                            <thead>
                              <tr>
                                <th scope="col">Webhook</th>
                                <th scope="col">Event</th>
                                <th scope="col">Status</th>
                                <th scope="col">Attempts</th>
                                <th scope="col">Last result</th>
                                <th scope="col">Queued</th>
                              </tr>
                            </thead>
                            <tbody>
                              {webhookDeliveries.map((delivery) => (
                                <tr key={delivery.id}>
                                  <th scope="row">{delivery.webhookName}</th>
                                  <td>{delivery.eventType}</td>
                                  <td>
                                    <span
                                      className={`delivery-status ${delivery.status.toLowerCase()}`}
                                    >
                                      {delivery.status}
                                    </span>
                                  </td>
                                  <td>{delivery.attemptCount}</td>
                                  <td>
                                    {delivery.lastStatusCode
                                      ? `HTTP ${delivery.lastStatusCode}`
                                      : delivery.lastError || "Waiting to send"}
                                    {delivery.lastError &&
                                      delivery.lastStatusCode && (
                                        <small className="delivery-error">
                                          {delivery.lastError}
                                        </small>
                                      )}
                                  </td>
                                  <td>
                                    {new Intl.DateTimeFormat(undefined, {
                                      dateStyle: "medium",
                                      timeStyle: "short",
                                    }).format(new Date(delivery.createdAt))}
                                  </td>
                                </tr>
                              ))}
                            </tbody>
                          </table>
                        </div>
                      )}
                    </section>
                  </section>
                )}
                {tab === "instances" && <AgentInstancesPanel instances={agentInstances} agents={agents} onRefresh={() => void loadAgentInstances()} />}
              </ProjectWorkspacePage>
            ))}
          {page === "chat" && <ConversationPage agentId={jimAgentId} projectId={jimProjectId} instances={jimInstances} activeInstanceId={activeInstanceId} timeline={timeline} initialPrompt={initialPrompt} outgoingMessage={outgoingMessage} messageDelivery={messageDelivery} isSending={isSendingMessage} onInitialPromptChange={setInitialPrompt} onOutgoingMessageChange={setOutgoingMessage} onMessageDeliveryChange={setMessageDelivery} onStart={startConversation} onSend={sendConversationMessage} onSelectInstance={setActiveInstanceId} onDeleteInstance={(instance) => void deleteConversation(instance)} />}
          {page === "llm" && <LlmConfigurationsPage configurations={llmConfigurations} editing={editingLlm} isFormOpen={isLlmFormOpen} isSaving={isSaving} name={llmName} providerUrl={providerUrl} modelName={modelName} apiKey={apiKey} onOpenForm={() => openLlmForm()} onSubmit={saveLlm} onCancel={() => setIsLlmFormOpen(false)} onNameChange={setLlmName} onProviderUrlChange={setProviderUrl} onModelNameChange={setModelName} onApiKeyChange={setApiKey} onEdit={openLlmForm} onDelete={(item) => void deleteLlm(item)} />}
          {page === "usage" && <UsagePage selectedProject={selectedProject} agents={agents} llmConfigurations={llmConfigurations} platformUsage={platformUsage} projectUsage={projectUsage} agentUsage={agentUsage} llmUsage={llmUsage} isLoading={isLoadingUsage} onRefresh={() => void loadUsage()} />}
        </main>
      </div>
      {viewingToolsFor && <McpToolsDialog server={viewingToolsFor} tools={mcpTools} isLoading={isLoadingMcpTools} error={mcpToolsError} onClose={() => setViewingToolsFor(null)} />}
    </div>
  );
}
