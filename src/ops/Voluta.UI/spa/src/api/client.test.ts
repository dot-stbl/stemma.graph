import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError, api, resolveUrl } from "./client";

function jsonResponse(body: unknown, status = 200): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: "TestStatus",
    text: () => Promise.resolve(JSON.stringify(body)),
  } as unknown as Response;
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("resolveUrl", () => {
  it("keeps the path when no base url is configured", () => {
    expect(resolveUrl("/voluta/api/threads")).toBe("/voluta/api/threads");
  });
});

describe("api error parsing", () => {
  it("returns parsed JSON on success", async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      jsonResponse([{ threadId: "t-1", status: "Done", step: 3 }]),
    );
    vi.stubGlobal("fetch", fetchMock);

    const threads = await api.listThreads();

    expect(threads).toEqual([{ threadId: "t-1", status: "Done", step: 3 }]);
    expect(fetchMock).toHaveBeenCalledWith(
      "/voluta/api/threads",
      expect.objectContaining({
        headers: expect.objectContaining({ Accept: "application/json" }),
      }),
    );
  });

  it("throws ApiError with detail from problem body", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue(
        jsonResponse({ detail: "thread not found", status: 404 }, 404),
      ),
    );

    const thrown = await api.getThread("missing").catch((e: unknown) => e);

    expect(thrown).toBeInstanceOf(ApiError);
    const error = thrown as ApiError;
    expect(error.status).toBe(404);
    expect(error.message).toBe("thread not found");
    expect(error.body).toEqual({ detail: "thread not found", status: 404 });
  });

  it("falls back through error and title fields", async () => {
    vi.stubGlobal(
      "fetch",
      vi
        .fn()
        .mockResolvedValueOnce(jsonResponse({ error: "upstream blew up" }, 502))
        .mockResolvedValueOnce(jsonResponse({ title: "Bad Gateway" }, 502)),
    );

    const first = await api.listThreads().catch((e: unknown) => e);
    expect((first as ApiError).message).toBe("upstream blew up");

    const second = await api.listThreads().catch((e: unknown) => e);
    expect((second as ApiError).message).toBe("Bad Gateway");
  });

  it("falls back to raw text body and status", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn().mockResolvedValue({
        ok: false,
        status: 500,
        statusText: "Internal Server Error",
        text: () => Promise.resolve("boom-plain"),
      } as unknown as Response),
    );

    const thrown = await api.listThreads().catch((e: unknown) => e);

    expect(thrown).toBeInstanceOf(ApiError);
    expect((thrown as ApiError).status).toBe(500);
    expect((thrown as ApiError).message).toBe("boom-plain");
  });

  it("sends POST with json content-type for resume", async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValue(jsonResponse({ kind: "approved", step: 4 }));
    vi.stubGlobal("fetch", fetchMock);

    const result = await api.resumeThread("t-1", { kind: "approve" });

    expect(result).toEqual({ kind: "approved", step: 4 });
    expect(fetchMock).toHaveBeenCalledWith(
      "/voluta/api/threads/t-1/resume",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ kind: "approve" }),
        headers: expect.objectContaining({
          "Content-Type": "application/json",
        }),
      }),
    );
  });
});
