import { NextRequest, NextResponse } from "next/server";
import { createHmac, timingSafeEqual } from "node:crypto";
import { auth } from "@clerk/nextjs/server";
import { authenticatedFetch } from "@/app/api/forward";
import { publish, subscribe, type AgentWebhookEvent } from "./event-broker";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

const agentEventTypes = new Set(["agent.message", "agent.tool_call"]);

export async function GET(request: NextRequest) {
  const { userId } = await auth();
  if (!userId) return NextResponse.json({ title: "An authenticated session is required." }, { status: 401 });

  const projectId = request.nextUrl.searchParams.get("projectId");
  const threadId = request.nextUrl.searchParams.get("threadId");
  if (!isGuid(projectId) || (threadId !== null && !isGuid(threadId)))
    return NextResponse.json({ title: "A valid projectId and optional threadId are required." }, { status: 400 });

  const apiUrl = process.env.API_INTERNAL_URL;
  if (!apiUrl) return NextResponse.json({ title: "The API service is not configured." }, { status: 503 });
  try {
    const project = await authenticatedFetch(`${apiUrl}/api/projects/${encodeURIComponent(projectId)}`, { cache: "no-store" });
    if (project.status === 404) return NextResponse.json({ title: "Project not found." }, { status: 404 });
    if (!project.ok) return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 });
  } catch {
    return NextResponse.json({ title: "The API service is unavailable." }, { status: 503 });
  }

  const encoder = new TextEncoder();
  let unsubscribe: (() => void) | undefined;
  let heartbeat: ReturnType<typeof setInterval> | undefined;

  const stream = new ReadableStream<Uint8Array>({
    start(controller) {
      const send = (event: AgentWebhookEvent) => {
        if (event.projectId !== projectId || (threadId && event.data.threadId !== threadId)) return;
        controller.enqueue(
          encoder.encode(`id: ${event.eventId}\nevent: ${event.eventType}\ndata: ${JSON.stringify(event)}\n\n`),
        );
      };
      controller.enqueue(encoder.encode(": connected\n\n"));
      unsubscribe = subscribe(send);
      heartbeat = setInterval(() => controller.enqueue(encoder.encode(": heartbeat\n\n")), 15_000);
      request.signal.addEventListener("abort", () => {
        unsubscribe?.();
        if (heartbeat) clearInterval(heartbeat);
      }, { once: true });
    },
    cancel() {
      unsubscribe?.();
      if (heartbeat) clearInterval(heartbeat);
    },
  });

  return new Response(stream, {
    headers: {
      "Cache-Control": "no-cache, no-transform",
      Connection: "keep-alive",
      "Content-Type": "text/event-stream",
      "X-Accel-Buffering": "no",
    },
  });
}

export async function POST(request: NextRequest) {
  const secret = process.env.AGENT_EVENTS_WEBHOOK_SECRET;
  if (!secret)
    return NextResponse.json({ title: "Agent event webhook is not configured." }, { status: 503 });

  const body = await request.text();
  const signature = request.headers.get("x-agent-platform-signature");
  const expected = `sha256=${createHmac("sha256", secret).update(body).digest("hex")}`;
  if (!signature || signature.length !== expected.length || !timingSafeEqual(Buffer.from(signature), Buffer.from(expected)))
    return NextResponse.json({ title: "Invalid webhook signature." }, { status: 401 });

  let event: AgentWebhookEvent;
  try {
    event = JSON.parse(body) as AgentWebhookEvent;
  } catch {
    return NextResponse.json({ title: "Webhook payload must be JSON." }, { status: 400 });
  }
  if (!event.eventId || !event.projectId || !agentEventTypes.has(event.eventType) || !event.data)
    return NextResponse.json({ title: "Unsupported webhook event." }, { status: 400 });

  publish(event);
  return NextResponse.json({ received: true });
}

function isGuid(value: string | null): value is string {
  return value !== null && /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}
