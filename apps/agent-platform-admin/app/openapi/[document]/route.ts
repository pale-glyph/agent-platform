import { NextResponse } from "next/server";

const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";

type RouteContext = { params: Promise<{ document: string }> };

export async function GET(_: Request, { params }: RouteContext) {
  const { document } = await params;

  try {
    const response = await fetch(`${apiUrl}/openapi/${encodeURIComponent(document)}`, {
      cache: "no-store",
    });

    return new NextResponse(response.body, {
      status: response.status,
      headers: {
        "Content-Type": response.headers.get("Content-Type") ?? "application/json",
      },
    });
  } catch {
    return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 });
  }
}
