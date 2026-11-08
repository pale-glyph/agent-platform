import { NextRequest, NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

async function forward(request: NextRequest, projectId: string, method: "GET" | "POST") {
  try {
    const response = await fetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}/webhooks/`, { method, body: method === "POST" ? await request.text() : undefined, headers: method === "POST" ? { "Content-Type": "application/json" } : undefined, cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch { return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 }); }
}

export async function GET(request: NextRequest, context: { params: Promise<{ projectId: string }> }) { return forward(request, (await context.params).projectId, "GET"); }
export async function POST(request: NextRequest, context: { params: Promise<{ projectId: string }> }) { return forward(request, (await context.params).projectId, "POST"); }
