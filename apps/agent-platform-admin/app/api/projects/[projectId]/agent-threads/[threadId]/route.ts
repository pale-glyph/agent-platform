import { NextRequest, NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

export async function DELETE(_: NextRequest, context: { params: Promise<{ projectId: string; threadId: string }> }) {
  try {
    const { projectId, threadId } = await context.params;
    const response = await fetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}/agent-threads/${encodeURIComponent(threadId)}`, { method: "DELETE", cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch { return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 }); }
}
