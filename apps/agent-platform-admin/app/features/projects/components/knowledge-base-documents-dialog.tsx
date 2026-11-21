"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import { readError } from "../../../services/http";
import type { KnowledgeBase, KnowledgeBaseDocument } from "../../../types";

type Props = { projectId: string; base: KnowledgeBase; onClose: () => void };
type Editor = { id: string | null; title: string; content: string };
const emptyEditor = (): Editor => ({ id: null, title: "", content: "" });

export function KnowledgeBaseDocumentsDialog({
  projectId,
  base,
  onClose,
}: Props) {
  const endpoint = `/api/projects/${projectId}/knowledge-bases/${base.id}/documents`;
  const [documents, setDocuments] = useState<KnowledgeBaseDocument[]>([]);
  const [editor, setEditor] = useState<Editor>(emptyEditor);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      const response = await fetch(endpoint);
      if (!response.ok) throw new Error(await readError(response));
      setDocuments((await response.json()) as KnowledgeBaseDocument[]);
    } catch (cause) {
      setError(
        cause instanceof Error
          ? cause.message
          : "Documents could not be loaded.",
      );
    } finally {
      setIsLoading(false);
    }
  }, [endpoint]);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void load();
    }, 0);
    return () => window.clearTimeout(timeoutId);
  }, [load]);
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, [onClose]);

  const selectDocument = async (document: KnowledgeBaseDocument) => {
    setError(null);
    try {
      const response = await fetch(`${endpoint}/${document.id}/content`);
      if (!response.ok) throw new Error(await readError(response));
      const { content } = (await response.json()) as { content: string };
      setEditor({ id: document.id, title: document.title, content });
    } catch (cause) {
      setError(
        cause instanceof Error
          ? cause.message
          : "Document content could not be loaded.",
      );
    }
  };

  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);
    setIsSaving(true);
    try {
      if (editor.id) {
        const metadataResponse = await fetch(`${endpoint}/${editor.id}`, {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ title: editor.title, metadata: {} }),
        });
        if (!metadataResponse.ok)
          throw new Error(await readError(metadataResponse));
        const contentResponse = await fetch(
          `${endpoint}/${editor.id}/content`,
          {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ content: editor.content }),
          },
        );
        if (!contentResponse.ok)
          throw new Error(await readError(contentResponse));
      } else {
        const response = await fetch(endpoint, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            title: editor.title,
            content: editor.content,
            metadata: {},
          }),
        });
        if (!response.ok) throw new Error(await readError(response));
      }
      await load();
      setEditor(emptyEditor());
    } catch (cause) {
      setError(
        cause instanceof Error ? cause.message : "Document could not be saved.",
      );
    } finally {
      setIsSaving(false);
    }
  };

  const remove = async () => {
    if (!editor.id || !window.confirm(`Delete “${editor.title}”?`)) return;
    setError(null);
    try {
      const response = await fetch(`${endpoint}/${editor.id}`, {
        method: "DELETE",
      });
      if (!response.ok) throw new Error(await readError(response));
      setEditor(emptyEditor());
      await load();
    } catch (cause) {
      setError(
        cause instanceof Error
          ? cause.message
          : "Document could not be deleted.",
      );
    }
  };

  return (
    <div
      className="modal-backdrop"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <section
        className="tools-modal knowledge-base-modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby="knowledge-base-documents-title"
      >
        <header className="tools-modal-heading">
          <div>
            <p className="eyebrow">Knowledge base</p>
            <h2 id="knowledge-base-documents-title">{base.name}</h2>
            <p>
              Manage source documents. Saved content is queued for indexing
              before agents retrieve it.
            </p>
          </div>
          <button
            className="modal-close"
            type="button"
            onClick={onClose}
            aria-label="Close document manager"
          >
            ×
          </button>
        </header>
        <div className="knowledge-base-content">
          <aside
            className="knowledge-base-document-list"
            aria-label="Documents"
          >
            <div>
              <strong>Documents</strong>
              <button
                className="text-button"
                type="button"
                onClick={() => setEditor(emptyEditor())}
              >
                New document
              </button>
            </div>
            {isLoading ? (
              <p>Loading documents…</p>
            ) : documents.length === 0 ? (
              <p>No documents yet.</p>
            ) : (
              <ul>
                {documents.map((document) => (
                  <li key={document.id}>
                    <button
                      type="button"
                      className={editor.id === document.id ? "selected" : ""}
                      onClick={() => void selectDocument(document)}
                    >
                      <strong>{document.title}</strong>
                      <small>{document.indexingStatus}</small>
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </aside>
          <form className="knowledge-base-editor" onSubmit={save}>
            <label htmlFor="document-title">Title</label>
            <input
              id="document-title"
              value={editor.title}
              maxLength={512}
              required
              onChange={(event) =>
                setEditor((current) => ({
                  ...current,
                  title: event.target.value,
                }))
              }
              placeholder="Document title"
            />
            <label htmlFor="document-content">Content</label>
            <textarea
              id="document-content"
              value={editor.content}
              required
              onChange={(event) =>
                setEditor((current) => ({
                  ...current,
                  content: event.target.value,
                }))
              }
              placeholder="Plain text content for agents to retrieve"
            />
            {error && <p role="alert">{error}</p>}
            <div className="knowledge-base-editor-actions">
              <button className="new-button" disabled={isSaving}>
                {isSaving
                  ? "Saving…"
                  : editor.id
                    ? "Save document"
                    : "Create document"}
              </button>
              {editor.id && (
                <button
                  type="button"
                  className="text-button danger-button"
                  onClick={() => void remove()}
                >
                  Delete document
                </button>
              )}
            </div>
          </form>
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
