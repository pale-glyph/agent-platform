import type { ProjectTab } from "../types";

export const webhookEventTypes = [
  ["project.updated", "Project updated"],
  ["agent.created", "Agent created"],
  ["agent.updated", "Agent updated"],
  ["agent.deleted", "Agent deleted"],
  ["agent.message", "Agent message"],
  ["agent.tool_call", "Agent tool call"],
  ["mcp_server.created", "MCP server created"],
  ["mcp_server.updated", "MCP server updated"],
  ["mcp_server.deleted", "MCP server deleted"],
] as const;

export const projectTabs: ReadonlyArray<{ id: ProjectTab; label: string }> = [
  { id: "overview", label: "Overview" },
  { id: "agents", label: "Agents" },
  { id: "servers", label: "Servers" },
  { id: "webhooks", label: "Webhooks" },
  { id: "instances", label: "Instances" },
];

export const defaultWebhookEvents = [
  "agent.created",
  "agent.updated",
  "agent.deleted",
  "agent.message",
  "agent.tool_call",
];
