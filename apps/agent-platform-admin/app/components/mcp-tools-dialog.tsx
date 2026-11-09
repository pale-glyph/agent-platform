import type { McpServer, McpTool } from "../types";

type McpToolsDialogProps = {
  server: McpServer;
  tools: McpTool[];
  isLoading: boolean;
  error: string | null;
  onClose: () => void;
};

export function McpToolsDialog({
  server,
  tools,
  isLoading,
  error,
  onClose,
}: McpToolsDialogProps) {
  return (
    <div
      className="modal-backdrop"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) onClose();
      }}
    >
      <section className="tools-modal" role="dialog" aria-modal="true" aria-labelledby="mcp-tools-title">
        <header className="tools-modal-heading">
          <div>
            <p className="eyebrow">MCP server tools</p>
            <h2 id="mcp-tools-title">{server.name}</h2>
            <p>{server.endpoint}</p>
          </div>
          <button className="modal-close" type="button" onClick={onClose} aria-label="Close tools dialog">×</button>
        </header>
        <div className="tools-modal-content" aria-live="polite">
          {isLoading ? <div className="empty-state"><strong>Loading tools…</strong><span>Fetching the server’s tool definitions.</span></div>
            : error ? <p className="status-message" role="alert">{error}</p>
              : tools.length === 0 ? <div className="empty-state"><strong>No tools found</strong><span>This server did not return any tool definitions.</span></div>
                : <div className="tools-table-wrap"><table className="tools-table"><thead><tr><th scope="col">Name</th><th scope="col">Description</th><th scope="col">Input schema</th></tr></thead><tbody>{tools.map((tool, index) => <tr key={`${tool.name}-${index}`}><th scope="row">{tool.name}</th><td>{tool.description || "—"}</td><td><pre>{JSON.stringify(tool.inputSchema ?? {}, null, 2)}</pre></td></tr>)}</tbody></table></div>}
        </div>
        <footer className="tools-modal-footer"><button className="text-button" type="button" onClick={onClose}>Close</button></footer>
      </section>
    </div>
  );
}
