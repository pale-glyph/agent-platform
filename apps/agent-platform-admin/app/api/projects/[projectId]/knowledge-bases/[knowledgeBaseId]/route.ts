import { authenticatedFetch } from "@/app/api/forward";
import { NextRequest, NextResponse } from "next/server";

const apiUrl =
  process.env.KNOWLEDGE_BASE_API_INTERNAL_URL ??
  "http://knowledge-base-api:8082";

type Context = {
  params: Promise<{ projectId: string; knowledgeBaseId: string }>;
};
async function forward(
  request: NextRequest,
  context: Context,
  method: "GET" | "PATCH" | "DELETE",
) {
  const { projectId, knowledgeBaseId } = await context.params;
  try {
    const response = await authenticatedFetch(
      `${apiUrl}/api/projects/${encodeURIComponent(projectId)}/knowledge-bases/${encodeURIComponent(knowledgeBaseId)}`,
      {
        method,
        body: method === "PATCH" ? await request.text() : undefined,
        headers:
          method === "PATCH"
            ? { "Content-Type": "application/json" }
            : undefined,
        cache: "no-store",
      },
    );
    return new NextResponse(response.body, {
      status: response.status,
      headers: {
        "Content-Type":
          response.headers.get("Content-Type") ?? "application/json",
      },
    });
  } catch {
    return NextResponse.json(
      { title: "The knowledge-base API is unavailable." },
      { status: 503 },
    );
  }
}

export async function GET(request: NextRequest, context: Context) {
  return forward(request, context, "GET");
}
export async function PATCH(request: NextRequest, context: Context) {
  return forward(request, context, "PATCH");
}
export async function DELETE(request: NextRequest, context: Context) {
  return forward(request, context, "DELETE");
}
