import { NextRequest, NextResponse } from "next/server";
const apiUrl = process.env.API_INTERNAL_URL ?? "http://localhost:8080";
async function forward(request: NextRequest, method: "GET" | "POST") { try { const response = await fetch(`${apiUrl}/api/llm-configurations/`, { method, body: method === "POST" ? await request.text() : undefined, headers: method === "POST" ? { "Content-Type": "application/json" } : undefined, cache: "no-store" }); return new NextResponse(response.body, { status: response.status, headers: { "Content-Type": response.headers.get("Content-Type") ?? "application/json" } }); } catch { return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 }); } }
export function GET(request: NextRequest) { return forward(request, "GET"); }
export function POST(request: NextRequest) { return forward(request, "POST"); }
