import { apiRoutes } from "./routes";
import type {
  ApiErrorBody,
  CheckpointSnapshot,
  ForkRequest,
  HitlThreadSummary,
  ResumeRequest,
  ResumeTerminal,
  StudioMutationResult,
  ThreadHistoryItem,
  ThreadSummary,
  TopologyDescription,
  UpdateStateRequest,
} from "./types";

/** Empty string = same origin (Vite proxy or host-served SPA). */
export function getApiBaseUrl(): string {
  const raw = import.meta.env.VITE_API_BASE_URL;
  if (raw === undefined || raw === null) {
    return "";
  }
  return String(raw).replace(/\/$/, "");
}

export function resolveUrl(path: string): string {
  return `${getApiBaseUrl()}${path}`;
}

export class ApiError extends Error {
  readonly status: number;
  readonly body: ApiErrorBody | string | null;

  constructor(status: number, message: string, body: ApiErrorBody | string | null = null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.body = body;
  }
}

async function parseBody(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) {
    return null;
  }
  try {
    return JSON.parse(text) as unknown;
  } catch {
    return text;
  }
}

async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(resolveUrl(path), {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  const body = await parseBody(response);

  if (!response.ok) {
    const message =
      typeof body === "object" && body !== null
        ? ((body as ApiErrorBody).detail ??
          (body as ApiErrorBody).error ??
          (body as ApiErrorBody).title ??
          response.statusText)
        : typeof body === "string" && body
          ? body
          : response.statusText;
    throw new ApiError(
      response.status,
      message || `HTTP ${response.status}`,
      body as ApiErrorBody | string | null,
    );
  }

  return body as T;
}

export const api = {
  listThreads: () => requestJson<ThreadSummary[]>(apiRoutes.threads()),

  getThread: (threadId: string) =>
    requestJson<CheckpointSnapshot>(apiRoutes.thread(threadId)),

  getHistory: (threadId: string) =>
    requestJson<ThreadHistoryItem[]>(apiRoutes.threadHistory(threadId)),

  listHitl: () => requestJson<HitlThreadSummary[]>(apiRoutes.hitl()),

  getTopology: () => requestJson<TopologyDescription>(apiRoutes.topology()),

  resumeThread: (threadId: string, body: ResumeRequest = {}) =>
    requestJson<ResumeTerminal>(apiRoutes.threadResume(threadId), {
      method: "POST",
      body: JSON.stringify(body),
    }),

  streamUrl: (
    threadId: string,
    mode: "checkpoint" | "resume" | "invoke" = "checkpoint",
  ) => resolveUrl(apiRoutes.threadStream(threadId, mode)),

  // /api/v1 studio contract (MapStudioApi). Continue takes no body.
  continueThread: (threadId: string) =>
    requestJson<StudioMutationResult>(apiRoutes.threadContinue(threadId), {
      method: "POST",
    }),

  updateThreadState: (threadId: string, body: UpdateStateRequest) =>
    requestJson<StudioMutationResult>(apiRoutes.threadUpdateState(threadId), {
      method: "POST",
      body: JSON.stringify(body),
    }),

  forkThread: (threadId: string, body: ForkRequest) =>
    requestJson<StudioMutationResult>(apiRoutes.threadFork(threadId), {
      method: "POST",
      body: JSON.stringify(body),
    }),
};
