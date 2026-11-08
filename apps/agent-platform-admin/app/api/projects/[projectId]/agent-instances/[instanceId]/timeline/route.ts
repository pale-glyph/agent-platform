import { NextRequest, NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

export async function GET(request: NextRequest, context: { params: Promise<{ projectId: string; instanceId: string }> }) {
  try {
    const { projectId, instanceId } = await context.params;
    const response = await fetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}/agent-instances/${encodeURIComponent(instanceId)}/timeline${request.nextUrl.search}`, { cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch { return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 }); }
}
