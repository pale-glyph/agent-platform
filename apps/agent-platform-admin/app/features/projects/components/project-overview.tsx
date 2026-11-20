"use client";

import { useState } from "react";
import type { Project } from "../../../types";

export function ProjectOverview({
  project,
  agentCount,
  serverCount,
}: {
  project: Project;
  agentCount: number;
  serverCount: number;
}) {
  const [copyStatus, setCopyStatus] = useState<"idle" | "copied" | "failed">(
    "idle",
  );

  const copyProjectId = async () => {
    try {
      await navigator.clipboard.writeText(project.id);
      setCopyStatus("copied");
    } catch {
      setCopyStatus("failed");
    }
  };

  return (
    <section className="project-panel tab-panel">
      <h2>Overview</h2>
      <dl className="project-details">
        <div>
          <dt>Project name</dt>
          <dd>{project.name}</dd>
        </div>
        <div>
          <dt>Project UUID</dt>
          <dd className="project-uuid">
            <code>{project.id}</code>
            <button
              type="button"
              onClick={() => void copyProjectId()}
              aria-label="Copy project UUID"
            >
              {copyStatus === "copied"
                ? "Copied"
                : copyStatus === "failed"
                  ? "Copy failed"
                  : "Copy UUID"}
            </button>
            <span className="sr-only" aria-live="polite">
              {copyStatus === "copied"
                ? "Project UUID copied to clipboard."
                : copyStatus === "failed"
                  ? "Project UUID could not be copied."
                  : ""}
            </span>
          </dd>
        </div>
        <div>
          <dt>Agent definitions</dt>
          <dd>{agentCount}</dd>
        </div>
        <div>
          <dt>MCP servers</dt>
          <dd>{serverCount}</dd>
        </div>
        <div>
          <dt>Created</dt>
          <dd>
            {new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(
              new Date(project.createdAt),
            )}
          </dd>
        </div>
      </dl>
    </section>
  );
}
