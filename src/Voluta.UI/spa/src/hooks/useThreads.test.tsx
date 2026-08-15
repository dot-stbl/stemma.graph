import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { useThreads } from "./useThreads";

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  const Wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
  return Wrapper;
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("useThreads", () => {
  it("resolves with the thread list on happy path", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: true,
        status: 200,
        statusText: "OK",
        text: () =>
          Promise.resolve(
            JSON.stringify([
              { threadId: "t-1", status: "Done", step: 2 },
              { threadId: "t-2", status: "Running", step: 1 },
            ]),
          ),
      } as unknown as Response),
    );

    const { result } = renderHook(() => useThreads(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data).toEqual([
      { threadId: "t-1", status: "Done", step: 2 },
      { threadId: "t-2", status: "Running", step: 1 },
    ]);
  });

  it("surfaces ApiError on failure", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 503,
        statusText: "Service Unavailable",
        text: () => Promise.resolve(JSON.stringify({ detail: "store down" })),
      } as unknown as Response),
    );

    const { result } = renderHook(() => useThreads(), {
      wrapper: createWrapper(),
    });

    await waitFor(() => expect(result.current.isError).toBe(true));

    expect((result.current.error as Error).message).toBe("store down");
    expect((result.current.error as { status?: number }).status).toBe(503);
  });
});
