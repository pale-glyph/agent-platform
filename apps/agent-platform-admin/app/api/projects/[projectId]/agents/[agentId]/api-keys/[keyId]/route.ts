import { authenticatedFetch } from "@/app/api/forward";
import { NextRequest, NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

export async function DELETE(request: NextRequest, context: { params: Promise<{ projectId: string; agentId: string; keyId: string }> }) {
  const { projectId, agentId, keyId } = await context.params;
  try {
    const response = await authenticatedFetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}/agents/${encodeURIComponent(agentId)}/api-keys/${encodeURIComponent(keyId)}`, { method: "DELETE", cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch { return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 }); }
}
