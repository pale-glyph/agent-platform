import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { Agent, AgentApiKey, CreatedAgentApiKey } from "../../../types";

type Props = {
  projectId: string;
  agent: Agent;
  onClose: () => void;
  onMessage: (message: string) => void;
};

export function AgentApiKeysDialog({
  projectId,
  agent,
  onClose,
  onMessage,
}: Props) {
  const [keys, setKeys] = useState<AgentApiKey[]>([]);
  const [name, setName] = useState("");
  const [created, setCreated] = useState<CreatedAgentApiKey | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);

  const endpoint = `/api/projects/${projectId}/agents/${agent.id}/api-keys/`;
  const refresh = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await fetch(endpoint);
      if (!response.ok) throw new Error("API keys could not be loaded.");
      setKeys((await response.json()) as AgentApiKey[]);
    } catch (error) {
      onMessage(
        error instanceof Error
          ? error.message
          : "API keys could not be loaded.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [endpoint, onMessage]);
  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void refresh();
    }, 0);
    return () => window.clearTimeout(timeoutId);
  }, [refresh]);
  const create = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    try {
      const response = await fetch(endpoint, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name }),
      });
      if (!response.ok) throw new Error("API key could not be created.");
      const key = (await response.json()) as CreatedAgentApiKey;
      setCreated(key);
      setName("");
      setKeys((current) => [...current, key]);
    } catch (error) {
      onMessage(
        error instanceof Error
          ? error.message
          : "API key could not be created.",
      );
    } finally {
      setIsSaving(false);
    }
  };
  const revoke = async (key: AgentApiKey) => {
    try {
      const response = await fetch(`${endpoint}${key.id}`, {
        method: "DELETE",
      });
      if (!response.ok) throw new Error("API key could not be revoked.");
      setKeys((current) => current.filter((item) => item.id !== key.id));
    } catch (error) {
      onMessage(
        error instanceof Error
          ? error.message
          : "API key could not be revoked.",
      );
    }
  };
  return (
    <div className="modal-backdrop" role="presentation">
      <section
        className="tools-modal api-keys-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="api-keys-title"
      >
        <header className="tools-modal-heading">
          <div>
            <p className="eyebrow">Agent API</p>
            <h2 id="api-keys-title">API keys for {agent.name}</h2>
            <p>Each key can access only this agent’s threads.</p>
          </div>
          <button
            className="modal-close"
            type="button"
            onClick={onClose}
            aria-label="Close API key manager"
          >
            ×
          </button>
        </header>
        <div className="api-keys-content">
          {created && (
            <div className="api-key-secret" role="status">
              <strong>Copy this key now. It will not be shown again.</strong>
              <code>{created.secret}</code>
              <button
                type="button"
                onClick={() =>
                  void navigator.clipboard.writeText(created.secret)
                }
              >
                Copy key
              </button>
            </div>
          )}
          <form className="api-key-form" onSubmit={create}>
            <label htmlFor="api-key-name">Key name</label>
            <input
              id="api-key-name"
              value={name}
              maxLength={200}
              placeholder="Production integration"
              onChange={(event) => setName(event.target.value)}
              required
            />
            <button className="new-button" disabled={isSaving}>
              {isSaving ? "Creating…" : "Create API key"}
            </button>
          </form>
          {isLoading ? (
            <p className="api-key-empty">Loading keys…</p>
          ) : keys.length === 0 ? (
            <p className="api-key-empty">
              No API keys have been created for this agent.
            </p>
          ) : (
            <ul className="api-key-list">
              {keys.map((key) => (
                <li key={key.id}>
                  <div>
                    <strong>{key.name}</strong>
                    <code>{key.keyPrefix}…</code>
                    <small>
                      Created {new Date(key.createdAt).toLocaleString()}
                    </small>
                  </div>
                  <button
                    className="text-button"
                    type="button"
                    onClick={() => void revoke(key)}
                  >
                    Revoke
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
        <footer className="tools-modal-footer">
          <button className="text-button" type="button" onClick={onClose}>
            Close
          </button>
        </footer>
      </section>
    </div>
  );
}
