# Agent Platform Contributor Guide

## Project intent

Agent Platform manages and executes asynchronous AI-agent work, including operator conversations and steering during an active run. The backend is .NET-based, PostgreSQL is the source of truth, and NATS JetStream provides durable asynchronous messaging.

## Architecture boundaries

- Keep domain rules independent of ASP.NET Core, Entity Framework Core, NATS, and other infrastructure libraries.
- Treat PostgreSQL as the authoritative record for agents, tasks, runs, and event history.
- Treat JetStream as a delivery mechanism, not as the only record of business state.
- Design all message handlers for at-least-once delivery: use stable message identifiers and idempotent state transitions.
- Keep HTTP/control-plane responsibilities separate from background execution workers.
- Model operator-to-agent communication as a durable, ordered conversation associated with a task or run; do not treat steering as an unpersisted side channel.
- Treat agent versions as immutable executable configurations. Pin every agent instance to one version and give it exactly one conversation thread.
- Make MCP access deny-by-default: an agent version may call a tool only through an explicit grant to a same-project MCP server.
- The built-in platform MCP server is an exception to project ownership, but not project scope: it must derive scope from the calling pinned agent version and require an explicit, tool-level grant. A platform administration agent is otherwise a regular project agent created through the UI.

## .NET conventions

- Use nullable reference types and asynchronous APIs for I/O.
- Pass `CancellationToken` through request handlers, background services, and external calls.
- Prefer dependency injection and small, testable services over static dependencies.
- Do not leak Entity Framework entities through API contracts; use dedicated request and response DTOs.
- Validate input at service boundaries and return problem details from HTTP endpoints.

## Administration UI conventions

- Build the administration application in `apps/agent-platform-admin` with Next.js, React, and TypeScript.
- Keep business rules in the .NET API. The browser must never connect directly to PostgreSQL or NATS.
- Keep agent-instance conversation and steering visible as tracked commands with delivery/outcome status, not optimistic chat-only state.
- Require accessible controls, keyboard support, and labelled asynchronous status feedback for administrative actions.

## Data and messaging

- Use migrations for every schema change; do not require manual production database edits.
- Persist the business state change and outbound message intent atomically (transactional outbox) before publishing asynchronous work.
- Acknowledge JetStream messages only after the corresponding durable work has succeeded or been safely recorded for retry.
- Record run lifecycle transitions as immutable events and reject invalid transitions.
- Persist a steering message before publishing its worker command. Workers must apply each command idempotently and record whether it was accepted, superseded, rejected, or expired.
- A run waiting for input must have a configured deadline and an explicit timeout outcome. Do not leave runs indefinitely waiting on an operator.
- Preserve an audit trail for human messages, agent questions, approvals, and steering decisions; redact sensitive content from logs.
- Publish external lifecycle events through the transactional outbox to the named JetStream targets configured on the pinned agent version. Include stable event and schema identifiers so applications can process pull-consumer deliveries idempotently.
- Route built-in platform MCP mutations through the control-plane application layer. Never allow the MCP server to write directly to PostgreSQL or publish directly to NATS.
- Never place credentials, provider keys, or sensitive task payloads in logs, source control, or example configuration.

## Verification

- Run `dotnet build AgentPlatform.sln`, `npm run typecheck` in `apps/agent-platform-admin`, formatting, and relevant tests before considering a change complete.
- Add unit tests for domain rules and integration tests for PostgreSQL/NATS boundaries when they are introduced.
- Update `README.md` when developer setup, configuration, architecture, or user-visible behavior changes.

## Local services

Start dependencies with `docker compose up -d`. PostgreSQL listens on port 5432, NATS client traffic on port 4222, and NATS monitoring on port 8222. Named volumes intentionally preserve local state; do not remove them unless a clean environment is required.
