"use client";

import { useWorkspaceNavigation } from "../../contexts/workspace-navigation-context";
import { ConversationPage } from "../../features/conversations/components/conversation-page";
import { useJimConversation } from "../../features/conversations/hooks/use-jim-conversation";
import { LlmConfigurationsPage } from "../../features/llm-configurations/components/llm-configurations-page";
import { useLlmConfigurations } from "../../features/llm-configurations/hooks/use-llm-configurations";
import { ProjectCreateForm } from "../../features/projects/components/project-create-form";
import { ProjectWorkspace } from "../../features/projects/components/project-workspace";
import { useProjectResources } from "../../features/projects/hooks/use-project-resources";
import { UsagePage } from "../../features/usage/components/usage-page";
import { useUsage } from "../../features/usage/hooks/use-usage";
import { AppHeader } from "./app-header";
import { AppSidebar } from "./app-sidebar";

export function AdminApp() {
  const workspace = useWorkspaceNavigation();
  const llmConfigurations = useLlmConfigurations(workspace.reportMessage);
  const projectResources = useProjectResources(
    workspace.selectedProjectId,
    workspace.reportMessage,
  );
  const conversation = useJimConversation({
    projects: workspace.projects,
    isActive: workspace.page === "chat",
    reportMessage: workspace.reportMessage,
  });
  const usage = useUsage({
    isActive: workspace.page === "usage",
    projectId: workspace.selectedProjectId,
    agents: projectResources.agents,
    llmConfigurations: llmConfigurations.configurations,
    reportMessage: workspace.reportMessage,
  });

  return (
    <div className="app-shell">
      <AppSidebar />
      <div className="workspace" id="workspace">
        <AppHeader onCreateLlm={() => llmConfigurations.openForm()} />
        <main>
          {workspace.message && (
            <p className="status-message" role="alert">
              {workspace.message}
            </p>
          )}
          {workspace.page === "projects" && workspace.isProjectFormOpen && (
            <ProjectCreateForm
              name={workspace.projectName}
              isSaving={workspace.isSavingProject}
              onNameChange={workspace.setProjectName}
              onSubmit={workspace.createProject}
              onCancel={workspace.closeProjectForm}
            />
          )}
          {workspace.page === "projects" &&
            (workspace.selectedProject ? (
              <ProjectWorkspace
                project={workspace.selectedProject}
                llmConfigurations={llmConfigurations.configurations}
                resources={projectResources}
              />
            ) : (
              <div className="empty-state workspace-empty">
                <strong>Select a project</strong>
                <span>Choose a project from the sidebar or create a new one to begin.</span>
              </div>
            ))}
          {workspace.page === "chat" && (
            <ConversationPage
              agentId={conversation.agentId}
              projectId={conversation.projectId}
              instances={conversation.instances}
              activeInstanceId={conversation.activeInstanceId}
              timeline={conversation.timeline}
              initialPrompt={conversation.initialPrompt}
              outgoingMessage={conversation.outgoingMessage}
              messageDelivery={conversation.messageDelivery}
              isSending={conversation.isSending}
              onInitialPromptChange={conversation.setInitialPrompt}
              onOutgoingMessageChange={conversation.setOutgoingMessage}
              onMessageDeliveryChange={conversation.setMessageDelivery}
              onStart={conversation.start}
              onSend={conversation.send}
              onSelectInstance={conversation.setActiveInstanceId}
              onDeleteInstance={(instance) => void conversation.remove(instance)}
            />
          )}
          {workspace.page === "llm" && (
            <LlmConfigurationsPage
              configurations={llmConfigurations.configurations}
              editing={llmConfigurations.editing}
              isFormOpen={llmConfigurations.isFormOpen}
              isSaving={llmConfigurations.isSaving}
              name={llmConfigurations.name}
              providerUrl={llmConfigurations.providerUrl}
              modelName={llmConfigurations.modelName}
              apiKey={llmConfigurations.apiKey}
              onOpenForm={() => llmConfigurations.openForm()}
              onSubmit={llmConfigurations.save}
              onCancel={llmConfigurations.closeForm}
              onNameChange={llmConfigurations.setName}
              onProviderUrlChange={llmConfigurations.setProviderUrl}
              onModelNameChange={llmConfigurations.setModelName}
              onApiKeyChange={llmConfigurations.setApiKey}
              onEdit={llmConfigurations.openForm}
              onDelete={(item) => void llmConfigurations.remove(item)}
            />
          )}
          {workspace.page === "usage" && (
            <UsagePage
              selectedProject={workspace.selectedProject}
              agents={projectResources.agents}
              llmConfigurations={llmConfigurations.configurations}
              platformUsage={usage.platformUsage}
              projectUsage={usage.projectUsage}
              agentUsage={usage.agentUsage}
              llmUsage={usage.llmUsage}
              isLoading={usage.isLoading}
              onRefresh={() => void usage.refresh()}
            />
          )}
        </main>
      </div>
    </div>
  );
}
