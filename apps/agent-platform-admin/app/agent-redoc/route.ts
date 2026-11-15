import { NextResponse } from "next/server";

const agentApiUrl = process.env.THREAD_API_INTERNAL_URL ?? "http://localhost:8081";

export async function GET() {
  try {
    const response = await fetch(`${agentApiUrl}/redoc`, { cache: "no-store" });
    const document = (await response.text()).replace(
      'spec-url="/openapi/v1.json"',
      'spec-url="/agent-openapi/v1.json"',
    );

    return new NextResponse(document, {
      status: response.status,
      headers: {
        "Content-Type": response.headers.get("Content-Type") ?? "text/html; charset=utf-8",
      },
    });
  } catch {
    return new NextResponse("The Agent API service is unavailable.", { status: 503 });
  }
}
