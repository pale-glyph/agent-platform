import { NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

export async function GET() {
  try {
    const response = await fetch(`${apiUrl}/redoc`, { cache: "no-store" });

    return new NextResponse(response.body, {
      status: response.status,
      headers: {
        "Content-Type": response.headers.get("Content-Type") ?? "text/html; charset=utf-8",
      },
    });
  } catch {
    return new NextResponse("The API service is unavailable.", { status: 503 });
  }
}
