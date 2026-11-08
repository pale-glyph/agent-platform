import { NextRequest, NextResponse } from "next/server";
import { createHmac, timingSafeEqual } from "node:crypto";
import { publish, subscribe, type AgentWebhookEvent } from "./event-broker";

export const runtime = "nodejs";
export const dynamic = "force-dynamic";

const agentEventTypes = new Set(["agent.message", "agent.tool_call"]);

export async function GET(request: NextRequest) {
  const encoder = new TextEncoder();
  let unsubscribe: (() => void) | undefined;
  let heartbeat: ReturnType<typeof setInterval> | undefined;

  const stream = new ReadableStream<Uint8Array>({
    start(controller) {
      const send = (event: AgentWebhookEvent) =>
        controller.enqueue(
          encoder.encode(`id: ${event.eventId}\nevent: ${event.eventType}\ndata: ${JSON.stringify(event)}\n\n`),
        );
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
