# System architecture

This diagram describes the current application containers and their principal connections. Dashed lines represent external dependencies or startup provisioning.

```mermaid
flowchart LR
    operator["Operator"] --> gateway["Traefik"] --> admin["Administration UI<br/>Next.js"]
    client["Agent API client"] --> agentapi["Agent API<br/>ASP.NET Core"]
    clerk["Clerk<br/>identity provider"] -.-> admin
    clerk -.-> frontend
    clerk -.-> kbapi

    subgraph platform["Agent Platform services"]
        frontend["Frontend API<br/>control plane and platform MCP"]
        agentapi
        worker["Execution worker"]
    end
    admin --> frontend
    admin --> agentapi
    admin --> kbapi
    frontend -->|"validate knowledge-base references"| kbapi

    subgraph knowledge["Knowledge Base service"]
        kbapi["Knowledge Base API"]
        embedding["Embedding worker"]
    end

    subgraph postgres["PostgreSQL server · one container"]
        platformdb[("agent_platform database<br/>agents, threads, grants, outbox")]
        kbdb[("knowledge_base database<br/>documents, revisions, chunks, jobs")]
    end
    frontend --> platformdb
    agentapi --> platformdb
    worker --> platformdb
    kbapi --> kbdb
    embedding -->|"poll indexing jobs and write chunks"| kbdb
    bootstrap["One-shot database bootstrap"] -.->|"create database if absent"| kbdb

    jetstream[("NATS JetStream")]
    frontend <--> jetstream
    agentapi <--> jetstream
    worker <--> jetstream
    worker --> llm["LLM provider"]
    worker --> mcp["External MCP servers"]
    worker --> webhook["Webhook destinations"]
```

The two databases share a PostgreSQL server but have separate service ownership. Project IDs in the knowledge-base database and knowledge-base IDs in platform grants are references; neither database uses foreign keys into the other. The knowledge-base worker currently polls its own durable indexing jobs; the diagram does not imply that it consumes JetStream messages.
