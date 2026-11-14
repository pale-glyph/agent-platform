export type AgentWebhookEvent = {
  eventId: string;
  projectId: string;
  eventType: "agent.message" | "agent.tool_call";
  data: { threadId?: string };
};

type Listener = (event: AgentWebhookEvent) => void;

type Broker = {
  listeners: Set<Listener>;
};

const brokerKey = "__agentPlatformEventBroker";

function getBroker(): Broker {
  const state = globalThis as typeof globalThis & { [brokerKey]?: Broker };
  return (state[brokerKey] ??= { listeners: new Set<Listener>() });
}

export function subscribe(listener: Listener): () => void {
  const broker = getBroker();
  broker.listeners.add(listener);
  return () => broker.listeners.delete(listener);
}

export function publish(event: AgentWebhookEvent): void {
  for (const listener of getBroker().listeners) listener(event);
}
