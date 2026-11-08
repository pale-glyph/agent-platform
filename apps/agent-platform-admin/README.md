# Agent Platform Administration UI

The Next.js application for administering projects, agents, MCP servers, agent instances, and execution runs.

## Development

```bash
npm install
npm run dev
```

The development server runs at `http://localhost:3000`. Configure the .NET API base URL through environment configuration once authenticated API calls are introduced.

## Container

From the repository root, run `./run.sh` to build this application along with the API and worker images and start the complete Docker Compose stack.

## Commands

```bash
npm run typecheck
npm run lint
npm run build
```
