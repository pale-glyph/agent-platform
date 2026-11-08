# Agent Platform Services

The .NET solution's API, worker, and shared application layers live in `src`. The solution remains at the repository root so it can share repository-wide build configuration.

Run `./run.sh` from the repository root to build the API and worker images and start them with the administration UI, PostgreSQL, and NATS.

For host-based development:

```bash
dotnet run --project apps/agent-platform-services/src/AgentPlatform.Api
dotnet run --project apps/agent-platform-services/src/AgentPlatform.Worker
```
