import type { FormEvent } from "react";
import type { AgentThread, ThreadEntry } from "../../../types";

type ConversationPageProps = {
  agentId: string;
  projectId: string;
  threads: AgentThread[];
  activeThreadId: string;
  entries: ThreadEntry[];
  initialPrompt: string;
  outgoingMessage: string;
  messageDelivery: "Queued" | "Steering";
  isSending: boolean;
  onInitialPromptChange: (value: string) => void;
  onOutgoingMessageChange: (value: string) => void;
  onMessageDeliveryChange: (value: "Queued" | "Steering") => void;
  onStart: (event: FormEvent<HTMLFormElement>) => void;
  onSend: (event: FormEvent<HTMLFormElement>) => void;
  onSelectThread: (id: string) => void;
  onDeleteThread: (thread: AgentThread) => void;
};

const formatDate = (value: string) => new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));

export function ConversationPage(props: ConversationPageProps) {
  if (!props.agentId) return <section className="project-panel tab-panel conversation-panel"><div className="empty-state compact-empty"><strong>Jim is not configured</strong><span>Set NEXT_PUBLIC_JIM_AGENT_ID to Jim’s agent UUID and restart the admin app.</span></div></section>;
  if (!props.projectId) return <section className="project-panel tab-panel conversation-panel"><div className="empty-state compact-empty"><strong>Finding Jim…</strong><span>Jim’s configured agent UUID was not found in an available project yet.</span></div></section>;

  return <section className="project-panel tab-panel conversation-panel">
    <div className="panel-heading"><div><p className="eyebrow">Assistant</p><h2>Jim</h2></div></div>
    <div className="conversation-layout">
      <aside className="conversation-list"><form onSubmit={props.onStart}><label htmlFor="initial-prompt">New conversation</label><textarea id="initial-prompt" value={props.initialPrompt} onChange={(event) => props.onInitialPromptChange(event.target.value)} placeholder="Ask Jim to configure a project." required /><button className="new-button" disabled={props.isSending}>{props.isSending ? "Starting…" : "Start conversation"}</button></form><div className="conversation-history" aria-label="Jim conversations">{props.threads.map((thread) => <div className="conversation-history-item" key={thread.id}><button className={thread.id === props.activeThreadId ? "active" : ""} type="button" onClick={() => props.onSelectThread(thread.id)}><strong>{formatDate(thread.createdAt)}</strong><small>{thread.status}</small></button><button aria-label="Delete conversation" className="conversation-delete" type="button" onClick={() => props.onDeleteThread(thread)}>Delete</button></div>)}</div></aside>
      <section className="conversation-thread" aria-live="polite">{!props.activeThreadId ? <div className="empty-state compact-empty"><strong>Talk to Jim</strong><span>Start a conversation to ask Jim to configure your projects.</span></div> : <><Timeline entries={props.entries} /><form className="conversation-compose" onSubmit={props.onSend}><label htmlFor="conversation-message">Message Jim</label><textarea id="conversation-message" value={props.outgoingMessage} onChange={(event) => props.onOutgoingMessageChange(event.target.value)} placeholder="Ask Jim to configure a project." required /><div><select aria-label="Message delivery" value={props.messageDelivery} onChange={(event) => props.onMessageDeliveryChange(event.target.value as "Queued" | "Steering")}><option value="Queued">Queue message</option><option value="Steering">Steer current request</option></select><button className="new-button" disabled={props.isSending}>{props.isSending ? "Sending…" : "Send"}</button></div></form></>}</section>
    </div>
  </section>;
}

function Timeline({ entries }: { entries: ThreadEntry[] }) {
  if (entries.length === 0) return <div className="empty-state compact-empty"><strong>Waiting for activity</strong><span>Jim’s execution timeline will appear here.</span></div>;
  return <ol>{entries.map((entry) => <li key={entry.id} className={`conversation-entry ${entry.type.toLowerCase()} ${entry.role === "User" ? "operator" : "agent"}`}>{entry.type === "ToolCall" ? <><strong>Tool call: {entry.toolName}</strong><pre>{entry.toolArguments}</pre>{entry.toolResult && <pre>{entry.toolResult}</pre>}</> : <><strong>{entry.role === "User" ? "You" : "Jim"}{entry.delivery && <small>{entry.delivery} · {entry.messageStatus}</small>}</strong><p>{entry.content}</p></>}</li>)}</ol>;
}
