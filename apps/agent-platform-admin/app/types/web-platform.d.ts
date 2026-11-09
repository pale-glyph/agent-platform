/**
 * TypeScript's DOM library does not yet declare these web-platform APIs,
 * although they are referenced by the installed Next.js version.
 */
interface PromiseWithResolvers<T> {
  promise: Promise<T>;
  resolve: (value: T | PromiseLike<T>) => void;
  reject: (reason?: unknown) => void;
}

type URLPatternInput = string | URLPatternInit;

interface URLPatternOptions {
  ignoreCase?: boolean;
}

interface URLPattern {
  test(input?: string | URL | URLPatternInit, baseURL?: string | URL): boolean;
}
