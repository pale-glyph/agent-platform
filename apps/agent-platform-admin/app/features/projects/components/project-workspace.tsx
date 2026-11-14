"use client";

import { McpToolsDialog } from "../../../components/dialogs/mcp-tools-dialog";
import { useWorkspaceNavigation } from "../../../contexts/workspace-navigation-context";
import type { LlmConfiguration, Project } from "../../../types";
import { projectTabs } from "../constants";
import { useAgentForm } from "../hooks/use-agent-form";
import { useMcpServerForm } from "../hooks/use-mcp-server-form";
import type { useProjectResources } from "../hooks/use-project-resources";
import { AgentThreadsPanel } from "./agent-threads-panel";
import { AgentsTab } from "./agents-tab";
import { ProjectOverview } from "./project-overview";
import { ProjectWorkspacePage } from "./project-workspace-page";
import { ServersTab } from "./servers-tab";
import { WebhooksTab } from "./webhooks-tab";

type ProjectWorkspaceProps = {
  project: Project;
  llmConfigurations: LlmConfiguration[];
  resources: ReturnType<typeof useProjectResources>;
};

export function ProjectWorkspace({
  project,
  llmConfigurations,
  resources,
}: ProjectWorkspaceProps) {
  const { tab, reportMessage } = useWorkspaceNavigation();
  const agentForm = useAgentForm({
    projectId: project.id,
    mcpServers: resources.mcpServers,
    onSaved: resources.refreshAgents,
    reportMessage,
  });
  const mcpServerForm = useMcpServerForm({
    projectId: project.id,
    onSaved: resources.refreshMcpServers,
    reportMessage,
  });

  return (
    <>
      <ProjectWorkspacePage project={project} tabs={projectTabs}>
        {tab === "overview" && (
          <ProjectOverview
            project={project}
            agentCount={resources.agents.length}
            serverCount={resources.mcpServers.length}
          />
        )}
        {tab === "agents" && (
          <AgentsTab
            project={project}
            agents={resources.agents}
            llmConfigurations={llmConfigurations}
            mcpServers={resources.mcpServers}
            editingAgent={agentForm.editingAgent}
            isFormOpen={agentForm.isFormOpen}
            isSaving={agentForm.isSaving}
            name={agentForm.name}
            description={agentForm.description}
            prompt={agentForm.prompt}
            llmConfigurationId={agentForm.llmConfigurationId}
            toolsByServer={agentForm.toolsByServer}
            selectedToolKeys={agentForm.selectedToolKeys}
            isLoadingTools={agentForm.isLoadingTools}
            onOpenForm={agentForm.openForm}
            onSubmit={agentForm.save}
            onCancel={agentForm.closeForm}
            onDelete={(agent) => void agentForm.remove(agent)}
            onNameChange={agentForm.setName}
            onDescriptionChange={agentForm.setDescription}
            onPromptChange={agentForm.setPrompt}
            onLlmConfigurationChange={agentForm.setLlmConfigurationId}
            onSelectedToolKeysChange={agentForm.setSelectedToolKeys}
          />
        )}
        {tab === "servers" && (
          <ServersTab
            servers={resources.mcpServers}
            editing={mcpServerForm.editing}
            isFormOpen={mcpServerForm.isFormOpen}
            isSaving={mcpServerForm.isSaving}
            name={mcpServerForm.name}
            endpoint={mcpServerForm.endpoint}
            cacheTtlSeconds={mcpServerForm.cacheTtlSeconds}
            security={mcpServerForm.security}
            apiKey={mcpServerForm.apiKey}
            onOpenForm={mcpServerForm.openForm}
            onSubmit={mcpServerForm.save}
            onCancel={mcpServerForm.closeForm}
            onNameChange={mcpServerForm.setName}
            onEndpointChange={mcpServerForm.setEndpoint}
            onCacheTtlSecondsChange={mcpServerForm.setCacheTtlSeconds}
            onSecurityChange={mcpServerForm.setSecurity}
            onApiKeyChange={mcpServerForm.setApiKey}
            onViewTools={(server) => void mcpServerForm.viewTools(server)}
            onClearCache={(server) => void mcpServerForm.clearCache(server)}
            onDelete={(server) => void mcpServerForm.remove(server)}
          />
        )}
        {tab === "webhooks" && (
          <WebhooksTab
            projectId={project.id}
            projectName={project.name}
            onMessage={reportMessage}
          />
        )}
        {tab === "threads" && (
          <AgentThreadsPanel
            threads={resources.agentThreads}
            agents={resources.agents}
            onRefresh={() => void resources.refreshAgentThreads()}
          />
        )}
      </ProjectWorkspacePage>
      {mcpServerForm.viewingToolsFor && (
        <McpToolsDialog
          server={mcpServerForm.viewingToolsFor}
          tools={mcpServerForm.tools}
          isLoading={mcpServerForm.isLoadingTools}
          error={mcpServerForm.toolsError}
          onClose={mcpServerForm.closeToolsDialog}
        />
      )}
    </>
  );
}
