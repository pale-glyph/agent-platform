import type { Project } from "../types";

export function ProjectOverview({ project, agentCount, serverCount }: { project: Project; agentCount: number; serverCount: number }) {
  return <section className="project-panel tab-panel"><h2>Overview</h2><dl className="project-details"><div><dt>Project name</dt><dd>{project.name}</dd></div><div><dt>Agent definitions</dt><dd>{agentCount}</dd></div><div><dt>MCP servers</dt><dd>{serverCount}</dd></div><div><dt>Created</dt><dd>{new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(project.createdAt))}</dd></div></dl></section>;
}
