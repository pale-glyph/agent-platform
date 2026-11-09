"use client";

import { useEffect, useState } from "react";
import { AppHeader } from "./components/app-header";
import { AppSidebar } from "./components/app-sidebar";
import { WorkspaceNavigationProvider } from "./components/workspace-navigation-context";
import { ProjectCreateForm } from "./components/project-create-form";
import { ConversationPage } from "./components/pages/conversation-page";
import { AgentsTab } from "./components/pages/agents-tab";
import { ServersTab } from "./components/pages/servers-tab";
import { WebhooksTab } from "./components/pages/webhooks-tab";
import { LlmConfigurationsPage } from "./components/pages/llm-configurations-page";
import { ProjectWorkspacePage } from "./components/pages/project-workspace-page";
import { AgentInstancesPanel } from "./components/agent-instances-panel";
import { McpToolsDialog } from "./components/mcp-tools-dialog";
import { ProjectOverview } from "./components/project-overview";
import { UsagePage } from "./components/pages/usage-page";
import {
  projectTabs,
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
} from "./types";

const jimAgentId = process.env.NEXT_PUBLIC_JIM_AGENT_ID ?? "";

export default function HomePage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [selectedProjectId, setSelectedProjectId] = useState("");
  const [tab, setTab] = useState<ProjectTab>("overview");
  const [agents, setAgents] = useState<Agent[]>([]);
  const [mcpServers, setMcpServers] = useState<McpServer[]>([]);
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
  }, [activeInstanceId, jimProjectId, page]);
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
    <WorkspaceNavigationProvider value={{ page, setPage, projects, selectedProject, selectedProjectId, isLoadingProjects: isLoading, selectProject, openProjectForm: () => setIsProjectFormOpen(true), openUsage: () => { setPage("usage"); void loadUsage(); }, tab, setTab }}>
    <div className="app-shell">
      <AppSidebar />

      <div className="workspace" id="workspace">
        <AppHeader onCreateLlm={() => openLlmForm()} />
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
              <ProjectWorkspacePage project={selectedProject} tabs={projectTabs}>
                {tab === "overview" && (
                  <ProjectOverview project={selectedProject} agentCount={agents.length} serverCount={mcpServers.length} />
                )}
                {tab === "agents" && <AgentsTab project={selectedProject} agents={agents} llmConfigurations={llmConfigurations} mcpServers={mcpServers} editingAgent={editingAgent} isFormOpen={isAgentFormOpen} isSaving={isSaving} name={agentName} description={agentDescription} prompt={agentPrompt} llmConfigurationId={agentLlmConfigurationId} toolsByServer={agentMcpTools} selectedToolKeys={selectedMcpToolKeys} isLoadingTools={isLoadingAgentMcpTools} onOpenForm={openAgentForm} onSubmit={saveAgent} onCancel={() => setIsAgentFormOpen(false)} onDelete={(agent) => void deleteAgent(agent)} onNameChange={setAgentName} onDescriptionChange={setAgentDescription} onPromptChange={setAgentPrompt} onLlmConfigurationChange={setAgentLlmConfigurationId} onSelectedToolKeysChange={setSelectedMcpToolKeys} />}
                {tab === "servers" && <ServersTab servers={mcpServers} editing={editingMcpServer} isFormOpen={isMcpServerFormOpen} isSaving={isSaving} name={mcpServerName} endpoint={mcpEndpoint} cacheTtlSeconds={mcpCacheTtlSeconds} security={mcpSecurity} apiKey={mcpApiKey} onOpenForm={openMcpServerForm} onSubmit={saveMcpServer} onCancel={() => setIsMcpServerFormOpen(false)} onNameChange={setMcpServerName} onEndpointChange={setMcpEndpoint} onCacheTtlSecondsChange={setMcpCacheTtlSeconds} onSecurityChange={setMcpSecurity} onApiKeyChange={setMcpApiKey} onViewTools={(server) => void viewMcpTools(server)} onClearCache={(server) => void clearMcpServerCache(server)} onDelete={(server) => void deleteMcpServer(server)} />}
                {tab === "webhooks" && <WebhooksTab projectId={selectedProject.id} projectName={selectedProject.name} onMessage={setMessage} />}
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
    </WorkspaceNavigationProvider>
  );
}
