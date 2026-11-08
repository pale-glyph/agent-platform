# Agent Platform Administration UI

The Next.js application for administering projects, agents, MCP servers, agent instances, and execution runs.

## UI organization

`app/page.tsx` owns client-side data loading and mutations. Shared API view models live in
`app/types.ts`; reusable shell and form controls are in `app/components`; and independent
application screens belong under `app/components/pages`.

## Development

```bash
npm install
npm run dev
```

The development server runs at `http://localhost:3000`. Configure the .NET API base URL through environment configuration once authenticated API calls are introduced.

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
