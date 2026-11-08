import { NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

export async function GET(request: Request, context: { params: Promise<{ id: string }> }) {
  try {
    const { id } = await context.params;
    const response = await fetch(`${apiUrl}/api/llm-configurations/${encodeURIComponent(id)}/usage${new URL(request.url).search}`, { cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch {
    return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 });
  }
}
