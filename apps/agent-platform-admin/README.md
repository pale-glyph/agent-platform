# Agent Platform Administration UI

The Next.js application for administering projects, agents, MCP servers, agent threads, and execution runs.

## UI organization

`app/page.tsx` owns client-side data loading and mutations. Shared API view models live in
`app/types.ts`; reusable shell and form controls are in `app/components`; and independent
application screens belong under `app/components/pages`.

## Development

```bash
npm install
npm run dev
```

The development server runs at `http://localhost:3000`. Configure the control-plane API base URL through `API_INTERNAL_URL` and the thread-service API base URL through `THREAD_API_INTERNAL_URL`. The administration header links to same-origin ReDoc documentation at `/redoc` and `/agent-redoc`; the frontend proxies each page and its OpenAPI document to the corresponding API service.

Clerk protects the UI and browser API routes. Set `NEXT_PUBLIC_CLERK_PUBLISHABLE_KEY` and `CLERK_SECRET_KEY` for the admin app, plus `CLERK_JWT_ISSUER` for the .NET services. On an authenticated API request, the backend records the Clerk subject in its `accounts` table and refreshes its `last_seen_at` timestamp.

Failed webhook deliveries can be removed from the delivery queue in the project Webhooks tab. This changes the delivery to `Cancelled` so it is no longer delivered or listed as failed, while preserving its audit record.

## Live agent updates

The admin receives live execution updates over Server-Sent Events. Create a project webhook with endpoint `http://admin:3000/api/agent-events`, subscribe it to `agent.message` and `agent.tool_call`, and set its signing secret to the same value as `AGENT_EVENTS_WEBHOOK_SECRET` on the admin container. The receiver verifies the platform HMAC signature before forwarding the event to connected browsers.

## Container

From the repository root, run `./run.sh` to build this application along with the API and worker images and start the complete Docker Compose stack.

## Commands

```bash
npm run typecheck
npm run lint
npm run build
```
