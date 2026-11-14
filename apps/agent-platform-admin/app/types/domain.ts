export type Project = { id: string; name: string; createdAt: string };

export type McpToolGrant = { mcpServerId: string; toolName: string };

export type Agent = {
  id: string;
  projectId: string;
  name: string;
  description: string;
  systemPrompt: string;
  llmConfigurationId: string | null;
  mcpToolGrants: McpToolGrant[];
  createdAt: string;
};

export type LlmConfiguration = {
  id: string;
  name: string;
  providerUrl: string;
  modelName: string;
  createdAt: string;
};

export type McpServer = {
  id: string;
  projectId: string;
  name: string;
  endpoint: string;
  cacheTtlSeconds: number;
  security: "None" | "ApiKey";
  cacheExpiresAt: string | null;
  createdAt: string;
};

export type McpTool = {
  name: string;
  description?: string;
  inputSchema?: unknown;
  annotations?: Record<string, unknown>;
};

export type Webhook = {
  id: string;
  projectId: string;
  name: string;
  endpointUrl: string;
  events: string[];
  hasSigningSecret: boolean;
  isEnabled: boolean;
  createdAt: string;
};

export type WebhookMetrics = {
  sent: number;
  failed: number;
  pending: number;
  attempts: number;
};

export type WebhookDelivery = {
  id: string;
  webhookId: string;
  webhookName: string;
  eventType: string;
  status: "Pending" | "Succeeded" | "Failed" | "Cancelled";
  attemptCount: number;
  lastStatusCode: number | null;
  lastError: string | null;
  createdAt: string;
  nextAttemptAt: string;
  completedAt: string | null;
};

export type AgentThread = {
  id: string;
  projectId: string;
  agentId: string;
  status: string;
  hasPendingQueuedMessage: boolean;
  cancellationRequestedAt: string | null;
  createdAt: string;
};

export type ThreadEntry = {
  id: string;
  threadId: string;
  sequenceNumber: number;
  type: "Message" | "ToolCall";
  role: "User" | "Agent" | null;
  delivery: "Initial" | "Queued" | "Steering" | null;
  messageStatus: "Pending" | "Delivered" | "Cancelled" | null;
  content: string | null;
  toolName: string | null;
  toolArguments: string | null;
  toolResult: string | null;
  createdAt: string;
};

export type ThreadPage = { items: ThreadEntry[]; nextCursor: number | null };

export type UsageBucket = {
  bucketStart: string;
  callCount: number;
  inputTokens: number;
  cachedInputTokens: number;
  outputTokens: number;
  reasoningTokens: number;
  totalTokens: number;
};

export type ProjectTab = "overview" | "agents" | "servers" | "webhooks" | "threads";
export type AdminPage = "projects" | "chat" | "llm" | "usage";
