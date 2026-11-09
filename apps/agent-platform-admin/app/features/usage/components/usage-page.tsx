import type { Agent, LlmConfiguration, Project, UsageBucket } from "../../../types";

type UsagePageProps = {
  selectedProject?: Project;
  agents: Agent[];
  llmConfigurations: LlmConfiguration[];
  platformUsage: UsageBucket[];
  projectUsage: UsageBucket[];
  agentUsage: Record<string, UsageBucket[]>;
  llmUsage: Record<string, UsageBucket[]>;
  isLoading: boolean;
  onRefresh: () => void;
};

export function UsagePage(props: UsagePageProps) {
  const { selectedProject, agents, llmConfigurations, platformUsage, projectUsage, agentUsage, llmUsage, isLoading, onRefresh } = props;
  return <section className="usage-page" aria-labelledby="usage-title">
    <div className="project-heading"><p className="eyebrow">Observability</p><h1 id="usage-title">LLM usage</h1><p>Successful model calls, grouped into 15-minute UTC windows.</p></div>
    <div className="usage-actions"><span aria-live="polite">{isLoading ? "Refreshing usage…" : ""}</span><button className="text-button" type="button" onClick={onRefresh} disabled={isLoading}>Refresh</button></div>
    <UsageSummary title="Platform" buckets={platformUsage} />
    <UsageSummary title={selectedProject ? `${selectedProject.name} project` : "Selected project"} buckets={projectUsage} />
    <section className="project-panel usage-breakdown"><div className="panel-heading"><div><p className="eyebrow">Agents</p><h2>Usage by agent</h2></div></div>{agents.length === 0 ? <div className="empty-state compact-empty"><strong>No agents in this project</strong></div> : <UsageBreakdown items={agents.map((agent) => ({ id: agent.id, label: agent.name, buckets: agentUsage[agent.id] ?? [] }))} />}</section>
    <section className="project-panel usage-breakdown"><div className="panel-heading"><div><p className="eyebrow">Models</p><h2>Usage by LLM configuration</h2></div></div>{llmConfigurations.length === 0 ? <div className="empty-state compact-empty"><strong>No LLM configurations</strong></div> : <UsageBreakdown items={llmConfigurations.map((llm) => ({ id: llm.id, label: `${llm.name} (${llm.modelName})`, buckets: llmUsage[llm.id] ?? [] }))} />}</section>
  </section>;
}

function UsageSummary({ title, buckets }: { title: string; buckets: UsageBucket[] }) {
  const totals = sumUsage(buckets);
  return <section className="project-panel usage-summary"><div className="panel-heading"><div><p className="eyebrow">Usage total</p><h2>{title}</h2></div><span>{buckets.length} windows</span></div><dl className="usage-metrics"><div><dt>Calls</dt><dd>{formatNumber(totals.callCount)}</dd></div><div><dt>Total tokens</dt><dd>{formatNumber(totals.totalTokens)}</dd></div><div><dt>Input tokens</dt><dd>{formatNumber(totals.inputTokens)}</dd></div><div><dt>Output tokens</dt><dd>{formatNumber(totals.outputTokens)}</dd></div><div><dt>Cached input</dt><dd>{formatNumber(totals.cachedInputTokens)}</dd></div><div><dt>Reasoning tokens</dt><dd>{formatNumber(totals.reasoningTokens)}</dd></div></dl>{buckets.length > 0 && <UsageWindows buckets={buckets} />}</section>;
}

function UsageBreakdown({ items }: { items: { id: string; label: string; buckets: UsageBucket[] }[] }) {
  return <div className="usage-table-wrap"><table className="usage-table"><thead><tr><th>Name</th><th>Calls</th><th>Total tokens</th><th>Input</th><th>Output</th><th>Cached</th><th>Reasoning</th></tr></thead><tbody>{items.map((item) => { const totals = sumUsage(item.buckets); return <tr key={item.id}><th>{item.label}</th><td>{formatNumber(totals.callCount)}</td><td>{formatNumber(totals.totalTokens)}</td><td>{formatNumber(totals.inputTokens)}</td><td>{formatNumber(totals.outputTokens)}</td><td>{formatNumber(totals.cachedInputTokens)}</td><td>{formatNumber(totals.reasoningTokens)}</td></tr>; })}</tbody></table></div>;
}

function UsageWindows({ buckets }: { buckets: UsageBucket[] }) {
  return <div className="usage-table-wrap"><table className="usage-table"><caption>Most recent 15-minute windows</caption><thead><tr><th>Window start (UTC)</th><th>Calls</th><th>Total tokens</th><th>Input</th><th>Output</th></tr></thead><tbody>{buckets.slice(-8).reverse().map((bucket) => <tr key={bucket.bucketStart}><th>{new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short", timeZone: "UTC" }).format(new Date(bucket.bucketStart))}</th><td>{formatNumber(bucket.callCount)}</td><td>{formatNumber(bucket.totalTokens)}</td><td>{formatNumber(bucket.inputTokens)}</td><td>{formatNumber(bucket.outputTokens)}</td></tr>)}</tbody></table></div>;
}

function sumUsage(buckets: UsageBucket[]) {
  return buckets.reduce((total, bucket) => ({ callCount: total.callCount + bucket.callCount, inputTokens: total.inputTokens + bucket.inputTokens, cachedInputTokens: total.cachedInputTokens + bucket.cachedInputTokens, outputTokens: total.outputTokens + bucket.outputTokens, reasoningTokens: total.reasoningTokens + bucket.reasoningTokens, totalTokens: total.totalTokens + bucket.totalTokens }), { callCount: 0, inputTokens: 0, cachedInputTokens: 0, outputTokens: 0, reasoningTokens: 0, totalTokens: 0 });
}

function formatNumber(value: number) { return new Intl.NumberFormat().format(value); }
