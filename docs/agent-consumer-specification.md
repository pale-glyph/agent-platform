# Agent Platform Integration Specification

This specification is for coding agents and applications that consume Agent Platform's HTTP API. It covers the chat/thread API and outbound webhooks. It reflects the current implementation; use the durable thread timeline as the source of truth for conversation state.

## Conventions

- Base API path: `/api`.
- IDs are UUIDs. Timestamps are ISO 8601 UTC timestamps.
- Request and response JSON uses camelCase. Enum values are strings, for example `"Queued"`.
- A validation failure is returned as HTTP `400` with problem details. A missing project, agent, or thread returns `404`.
- The current API has no application-level authentication. Deploy it behind an authenticated, authorized gateway before exposing it outside a trusted network.

Create project and agent configuration through the administration UI or the project/agent endpoints before starting a chat. An agent must have an LLM configuration before it can execute.

## Chat API

### Start a thread

```http
POST /api/projects/{projectId}/agents/{agentId}/threads
Content-Type: application/json

{ "initialPrompt": "Summarize the open incidents." }
```

The response is `201 Created` and contains a thread summary:

```json
{
  "id": "8cae8b64-f07d-47e6-94e7-02b5b1d98c77",
  "projectId": "...",
  "agentId": "...",
  "status": "Running",
  "hasPendingQueuedMessage": false,
  "cancellationRequestedAt": null,
  "createdAt": "2026-11-14T12:00:00+00:00"
}
```

The initial prompt is stored as the first timeline entry. Thread execution is asynchronous; creation does not mean an agent response has already been produced.

### Read threads and timeline

```http
GET /api/projects/{projectId}/agent-threads
GET /api/projects/{projectId}/agent-threads/{threadId}/timeline?afterSequence=0&limit=50
```

`afterSequence` is an exclusive cursor. `limit` is optional (default `50`) and must be from 1 through 100. The timeline endpoint returns:

```json
{
  "items": [
    {
      "id": "...",
      "threadId": "...",
      "sequenceNumber": 1,
      "type": "Message",
      "role": "User",
      "delivery": "Initial",
      "messageStatus": "Delivered",
      "content": "Summarize the open incidents.",
      "toolName": null,
      "toolArguments": null,
      "toolResult": null,
      "createdAt": "2026-11-14T12:00:00+00:00"
    }
  ],
  "nextCursor": null
}
```

When `nextCursor` is non-null, request the next page with that value as `afterSequence`. Persist the highest processed `sequenceNumber`; this makes polling and webhook reconciliation idempotent.

Timeline entry fields are:

| Field | Meaning |
| --- | --- |
| `type` | `Message` or `ToolCall`. |
| `role` | `User` or `Agent` for messages; `null` for tool calls. |
| `delivery` | `Initial`, `Queued`, or `Steering` for messages; `null` for tool calls. |
| `messageStatus` | `Pending`, `Delivered`, or `Cancelled` for messages; `null` for tool calls. |
| `content` | Message text; `null` for tool calls. |
| `toolName`, `toolArguments`, `toolResult` | Tool-call data; all are `null` for messages. Tool arguments and results are JSON encoded as strings. |

### Send a message

```http
POST /api/projects/{projectId}/agent-threads/{threadId}/messages
Content-Type: application/json

{
  "content": "Include only customer-impacting incidents.",
  "delivery": "Queued"
}
```

The response is `202 Accepted`:

```json
{
  "entry": { "id": "...", "sequenceNumber": 2, "...": "..." },
  "cancelledInFlightRequest": false
}
```

`delivery` must be either `Queued` or `Steering`; `Initial` is reserved for thread creation. Content is required and limited to 100,000 characters.

Use `Queued` for the next agent turn. It is persisted with `messageStatus: "Pending"` and becomes `Delivered` when the worker takes it up. Only one queued message may be pending per thread; submitting another returns `409 Conflict`.

Use `Steering` for an instruction that should apply to active work. It is persisted immediately as `Delivered`. If the thread was running, `cancelledInFlightRequest: true` means the platform recorded a cooperative cancellation request; clients should still poll the timeline and thread status for the eventual outcome. If a completed thread receives either delivery mode, it is resumed.

### Chat client pattern

1. Start a thread and save its ID.
2. Poll the timeline from the last processed sequence number, or receive webhooks and reconcile them against the timeline.
3. Render an agent response only after receiving an `Agent` message with `messageStatus: "Delivered"`.
4. Send at most one queued follow-up while a queued message remains pending; use steering only when interrupting or redirecting active work is intended.
5. Treat tool calls as audit events, not as user-facing assistant messages unless the product intentionally surfaces them.

## Webhooks

Register a project webhook to receive timeline and configuration events:

```http
POST /api/projects/{projectId}/webhooks
Content-Type: application/json

{
  "name": "Consumer event receiver",
  "endpointUrl": "https://consumer.example.com/agent-platform/events",
  "secret": "a-secret-at-least-32-characters-long",
  "events": ["agent.message", "agent.tool_call"]
}
```

`endpointUrl` must be an absolute HTTP or HTTPS URL. `secret` is required, must be 32–1,000 characters, and is used to sign the delivery. Register only the events your consumer needs:

| Event type | When emitted |
| --- | --- |
| `agent.message` | An initial prompt, operator message, or agent message is stored. |
| `agent.tool_call` | A tool invocation is recorded. |
| `project.updated` | A project is updated. |
| `agent.created`, `agent.updated`, `agent.deleted` | An agent changes. |
| `mcp_server.created`, `mcp_server.updated`, `mcp_server.deleted` | An MCP-server registration changes. |

The platform posts JSON to the registered endpoint with these headers:

```http
Content-Type: application/json
X-Agent-Platform-Event-Id: <UUID>
X-Agent-Platform-Event-Type: agent.message
X-Agent-Platform-Signature: sha256=<lowercase hex HMAC-SHA-256>
```

The signature is the lowercase hexadecimal HMAC-SHA-256 of the exact UTF-8 request body, keyed by the configured webhook secret. Verify it in constant time before parsing or acting on the payload. Do not reserialize the body before verification.

```json
{
  "eventId": "2f0d9d44-4a53-47e2-bf36-27f49dd8e31a",
  "schemaVersion": "1.0",
  "projectId": "...",
  "eventType": "agent.message",
  "occurredAt": "2026-11-14T12:00:00+00:00",
  "data": {
    "id": "...",
    "threadId": "...",
    "sequenceNumber": 3,
    "type": "Message",
    "role": "Agent",
    "delivery": "Initial",
    "messageStatus": "Delivered",
    "content": "There are two customer-impacting incidents.",
    "toolName": null,
    "toolArguments": null,
    "toolResult": null,
    "createdAt": "2026-11-14T12:00:00+00:00"
  }
}
```

For `agent.tool_call`, `data` uses the same timeline-entry shape, with `type: "ToolCall"` and its `toolName`, `toolArguments`, and `toolResult` populated.

### Delivery semantics

Webhook delivery is at least once. Deduplicate by `eventId` (and use `threadId` plus `sequenceNumber` to preserve chat ordering). Return any `2xx` response only after the event is durably recorded by the receiver.

- HTTP `408`, `429`, and `5xx`, connection failures, and timeouts are retried up to 10 attempts.
- Other non-2xx responses fail the delivery without automatic retry.
- Retries use exponential backoff, starting at 5 seconds and capped at one hour.
- After five consecutive endpoint failures, the platform opens a one-minute circuit breaker for that endpoint.

Manage registrations and recover failed deliveries with:

| Operation | Endpoint |
| --- | --- |
| List webhooks | `GET /api/projects/{projectId}/webhooks` |
| Create webhook | `POST /api/projects/{projectId}/webhooks` |
| Update webhook | `PUT /api/projects/{projectId}/webhooks/{webhookId}` |
| Delete webhook | `DELETE /api/projects/{projectId}/webhooks/{webhookId}` |
| Read delivery metrics | `GET /api/projects/{projectId}/webhooks/metrics` |
| List pending/failed deliveries | `GET /api/projects/{projectId}/webhooks/deliveries` |
| Retry failed deliveries | `POST /api/projects/{projectId}/webhooks/deliveries/retry-failed` |

## Consumer acceptance checklist

- Store thread IDs and process timeline entries in sequence-number order.
- Handle `202 Accepted` as asynchronous, not as an agent response.
- Do not submit a second queued message when `hasPendingQueuedMessage` is true.
- Verify every webhook signature against the raw request bytes in constant time.
- Deduplicate webhook events by `eventId` and reconcile missed or out-of-order events from the timeline API.
- Return a `2xx` response only after durable processing; use a `5xx` response when a retry is required.
- Treat webhook and timeline payloads as potentially sensitive because they can include operator prompts, model output, and tool arguments/results.
