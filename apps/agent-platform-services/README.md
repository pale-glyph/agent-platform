# Agent Platform Services

The .NET solution's API, worker, and shared application layers live in `src`. The solution remains at the repository root so it can share repository-wide build configuration.

Run `./run.sh` from the repository root to build the API and worker images and start them with the administration UI, PostgreSQL, and NATS.

For host-based development:

```bash
dotnet run --project apps/agent-platform-services/src/AgentPlatform.Api
dotnet run --project apps/agent-platform-services/src/AgentPlatform.Worker
```

The API also exposes a Model Context Protocol endpoint at `POST /mcp`. It supports initialization, tool discovery, and the `list_projects` tool. The built-in no-op webhook receiver is `POST /webhooks/receive`; it consumes the request body and returns HTTP 200. Project webhooks can subscribe to project updates and agent/MCP-server lifecycle events. Each queued delivery snapshots its receiver and signing secret in the same PostgreSQL save as the business change. The API's hosted dispatcher signs the exact JSON request body with HMAC-SHA256, sends the lowercase hexadecimal digest as `sha256=<digest>` in `X-Agent-Platform-Signature`, and includes `X-Agent-Platform-Event-Id` and `X-Agent-Platform-Event-Type` headers.
