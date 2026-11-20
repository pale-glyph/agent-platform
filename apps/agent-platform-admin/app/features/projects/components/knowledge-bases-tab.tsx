"use client";
import { useEffect, useState, type FormEvent } from "react";
import type { KnowledgeBase } from "../../../types";
import { readError } from "../../../services/http";
export function KnowledgeBasesTab({
  projectId,
  onChanged,
}: {
  projectId: string;
  onChanged: (bases: KnowledgeBase[]) => void;
}) {
  const [bases, setBases] = useState<KnowledgeBase[]>([]);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [open, setOpen] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const load = async () => {
    const response = await fetch(`/api/projects/${projectId}/knowledge-bases/`);
    if (!response.ok) throw new Error(await readError(response));
    const values = (await response.json()) as KnowledgeBase[];
    setBases(values);
    onChanged(values);
  };
  useEffect(() => {
    void load().catch((e) =>
      setError(
        e instanceof Error ? e.message : "Knowledge bases could not be loaded.",
      ),
    );
  }, [projectId]);
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    try {
      const response = await fetch(
        `/api/projects/${projectId}/knowledge-bases/`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ name, description: description || null }),
        },
      );
      if (!response.ok) throw new Error(await readError(response));
      setName("");
      setDescription("");
      setOpen(false);
      await load();
    } catch (e) {
      setError(
        e instanceof Error ? e.message : "Knowledge base could not be created.",
      );
    }
  };
  return (
    <section className="project-panel tab-panel">
      <div className="panel-heading">
        <div>
          <p className="eyebrow">Knowledge bases</p>
          <h2>Project retrieval sources</h2>
          <p>
            Grant each knowledge base to individual agents from the Agents tab.
          </p>
        </div>
        <button className="new-button" onClick={() => setOpen(true)}>
          New knowledge base
        </button>
      </div>
      {error && <p role="alert">{error}</p>}
      {open && (
        <form className="agent-form" onSubmit={submit}>
          <label>
            Name
            <input
              value={name}
              maxLength={200}
              onChange={(e) => setName(e.target.value)}
              required
            />
          </label>
          <label>
            Description
            <textarea
              value={description}
              maxLength={4000}
              onChange={(e) => setDescription(e.target.value)}
            />
          </label>
          <div>
            <button className="new-button">Create</button>
            <button
              type="button"
              className="text-button"
              onClick={() => setOpen(false)}
            >
              Cancel
            </button>
          </div>
        </form>
      )}
      {bases.length === 0 ? (
        <div className="empty-state">
          <strong>No knowledge bases yet</strong>
          <span>Add plain-text sources for agents to retrieve.</span>
        </div>
      ) : (
        <ul className="project-list">
          {bases.map((base) => (
            <li key={base.id}>
              <span className="project-badge">
                {base.name.slice(0, 1).toUpperCase()}
              </span>
              <div>
                <strong>{base.name}</strong>
                <small>{base.description ?? "No description"}</small>
                <small>{base.status}</small>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
