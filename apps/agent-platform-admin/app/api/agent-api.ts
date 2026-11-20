import { auth } from "@clerk/nextjs/server";

export class AgentApiConfigurationError extends Error {}
export class AgentApiAuthenticationError extends Error {}

export async function jimAgentFetch(
  input: RequestInfo | URL,
  init?: RequestInit,
) {
  const { userId } = await auth();
  if (!userId)
    throw new AgentApiAuthenticationError(
      "An authenticated Clerk session is required.",
    );

  const apiKey = process.env.JIM_AGENT_API_KEY;
  if (!apiKey)
    throw new AgentApiConfigurationError(
      "Jim's agent API key is not configured.",
    );

  const headers = new Headers(init?.headers);
  headers.set("Authorization", `Bearer ${apiKey}`);
  return fetch(input, { ...init, headers });
}
