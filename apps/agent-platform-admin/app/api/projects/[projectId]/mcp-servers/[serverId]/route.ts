import { NextRequest, NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

async function forward(request: NextRequest, projectId: string, serverId: string, method: "PUT" | "DELETE") {
  try {
    const response = await fetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}/mcp-servers/${encodeURIComponent(serverId)}`, { method, body: method === "PUT" ? await request.text() : undefined, headers: method === "PUT" ? { "Content-Type": "application/json" } : undefined, cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch { return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 }); }
}

export async function GET(_request: NextRequest, context: { params: Promise<{ projectId: string; serverId: string }> }) {
  const { projectId, serverId } = await context.params;
  try {
    const response = await fetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}/mcp-servers/${encodeURIComponent(serverId)}/tools`, { cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch { return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 }); }
}

export async function PUT(request: NextRequest, context: { params: Promise<{ projectId: string; serverId: string }> }) { const { projectId, serverId } = await context.params; return forward(request, projectId, serverId, "PUT"); }
export async function DELETE(request: NextRequest, context: { params: Promise<{ projectId: string; serverId: string }> }) { const { projectId, serverId } = await context.params; return forward(request, projectId, serverId, "DELETE"); }
