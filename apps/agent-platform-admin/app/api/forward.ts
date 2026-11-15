import { auth } from "@clerk/nextjs/server";

export async function authenticatedFetch(input: RequestInfo | URL, init?: RequestInit) {
  const { getToken } = await auth();
  const token = await getToken();
  if (!token) {
    throw new Error("An authenticated Clerk session is required.");
  }

  const headers = new Headers(init?.headers);
  headers.set("Authorization", `Bearer ${token}`);
  return fetch(input, { ...init, headers });
}
