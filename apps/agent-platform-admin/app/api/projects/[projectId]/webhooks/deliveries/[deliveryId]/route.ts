import { authenticatedFetch } from "@/app/api/forward";
import { NextRequest, NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

export async function DELETE(_request: NextRequest, context: { params: Promise<{ projectId: string; deliveryId: string }> }) {
  try {
    const { projectId, deliveryId } = await context.params;
    const response = await authenticatedFetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}/webhooks/deliveries/${encodeURIComponent(deliveryId)}`, { method: "DELETE", cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch {
    return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 });
  }
}
