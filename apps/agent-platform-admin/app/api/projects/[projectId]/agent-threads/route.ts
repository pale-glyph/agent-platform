import { AgentApiAuthenticationError, AgentApiConfigurationError, jimAgentFetch } from "@/app/api/agent-api";
import { NextRequest, NextResponse } from "next/server";

const apiUrl = process.env.THREAD_API_INTERNAL_URL ?? "http://localhost:8081";

export async function GET(_: NextRequest, context: { params: Promise<{ projectId: string }> }) {
  try {
    const { projectId } = await context.params;
    const response = await jimAgentFetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}/agent-threads/`, { cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch (error) {
    if (error instanceof AgentApiAuthenticationError) return NextResponse.json({ title: error.message }, { status: 401 });
    if (error instanceof AgentApiConfigurationError) return NextResponse.json({ title: error.message }, { status: 503 });
    return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 });
  }
}
