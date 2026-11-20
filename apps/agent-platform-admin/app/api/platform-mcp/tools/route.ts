import { authenticatedFetch } from "@/app/api/forward";
import { NextResponse } from "next/server";

export async function GET() {
  try {
    const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";
    const response = await authenticatedFetch(`${apiUrl}/api/platform-mcp/tools`, { cache: "no-store" });
    return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } });
  } catch {
    return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 });
  }
}
