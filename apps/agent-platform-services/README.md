# Agent Platform Services

The .NET solution's API, worker, and shared application layers live in `src`. The solution remains at the repository root so it can share repository-wide build configuration.

Run `./run.sh` from the repository root to build the API and worker images and start them with the administration UI, PostgreSQL, and NATS.

For host-based development:

```bash
dotnet run --project apps/agent-platform-services/src/AgentPlatform.Api
dotnet run --project apps/agent-platform-services/src/AgentPlatform.Worker
```

Platform database connections use password authentication and explicitly disable PostgreSQL GSS encryption; Kerberos libraries and configuration are not required for the local stack.

## Agent runtime

The worker polls running agent threads and invokes the configured provider using the OpenAI Chat Completions-compatible `POST /v1/chat/completions` contract. Set an LLM configuration's provider URL to either the provider base URL (for example `https://api.openai.com`) or the full chat-completions URL. The worker appends `/v1/chat/completions` when a base URL is supplied.

The runtime sends the agent system prompt and persisted conversation to the model. It exposes only the MCP tool names explicitly granted to that agent, forwards tool calls to the matching MCP server with JSON-RPC `tools/call`, persists every call and result, and returns those results to the model. A run is capped at eight tool-call rounds and is completed only after a final assistant response is persisted.

The API also exposes a Model Context Protocol endpoint at `POST /mcp`. It supports initialization, tool discovery, and the `list_projects` tool. The built-in no-op webhook receiver is `POST /webhooks/receive`; it consumes the request body and returns HTTP 200. Project webhooks use HTTPS, except for loopback receivers and the internal Docker Compose `admin` service. Each queued delivery snapshots its receiver and signing secret in the same PostgreSQL save as the business change. The API's hosted dispatcher signs the exact JSON request body with HMAC-SHA256, sends the lowercase hexadecimal digest as `sha256=<digest>` in `X-Agent-Platform-Signature`, and includes `X-Agent-Platform-Event-Id` and `X-Agent-Platform-Event-Type` headers.
