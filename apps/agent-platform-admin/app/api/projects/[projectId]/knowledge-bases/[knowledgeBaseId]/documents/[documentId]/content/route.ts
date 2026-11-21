import { authenticatedFetch } from "@/app/api/forward";
import { NextRequest, NextResponse } from "next/server";

const apiUrl =
  process.env.KNOWLEDGE_BASE_API_INTERNAL_URL ??
  "http://knowledge-base-api:8082";
type Context = {
  params: Promise<{
    projectId: string;
    knowledgeBaseId: string;
    documentId: string;
  }>;
};

export async function GET(request: NextRequest, context: Context) {
  const { projectId, knowledgeBaseId, documentId } = await context.params;
  try {
    const response = await authenticatedFetch(
      `${apiUrl}/api/projects/${encodeURIComponent(projectId)}/knowledge-bases/${encodeURIComponent(knowledgeBaseId)}/documents/${encodeURIComponent(documentId)}/content`,
      { cache: "no-store" },
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

export async function PUT(request: NextRequest, context: Context) {
  const { projectId, knowledgeBaseId, documentId } = await context.params;
  try {
    const response = await authenticatedFetch(
      `${apiUrl}/api/projects/${encodeURIComponent(projectId)}/knowledge-bases/${encodeURIComponent(knowledgeBaseId)}/documents/${encodeURIComponent(documentId)}/content`,
      {
        method: "PUT",
        body: await request.text(),
        headers: { "Content-Type": "application/json" },
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
