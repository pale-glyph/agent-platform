export async function readError(response: Response) {
  const body = (await response.json().catch(() => null)) as {
    errors?: Record<string, string[]>;
    title?: string;
  } | null;

  return body?.errors
    ? Object.values(body.errors).flat()[0]
    : (body?.title ?? "The request could not be saved.");
}
