/** Path builders. Legacy ops API under `/voluta/api`; studio v1 at `/api/v1`. */

const API_ROOT = "/voluta/api";
const API_ROOT_V1 = "/api/v1";

export const apiRoutes = {
  topology: () => `${API_ROOT}/topology`,
  hitl: () => `${API_ROOT}/hitl`,
  threads: () => `${API_ROOT}/threads`,
  thread: (threadId: string) => `${API_ROOT}/threads/${encodeURIComponent(threadId)}`,
  threadHistory: (threadId: string) =>
    `${API_ROOT}/threads/${encodeURIComponent(threadId)}/history`,
  threadResume: (threadId: string) => `${API_ROOT}/threads/${encodeURIComponent(threadId)}/resume`,
  threadStream: (
    threadId: string,
    mode: "checkpoint" | "resume" | "invoke" = "checkpoint",
  ) =>
    `${API_ROOT}/threads/${encodeURIComponent(threadId)}/stream?mode=${encodeURIComponent(mode)}`,
  // /api/v1 studio contract (MapStudioApi).
  threadContinue: (threadId: string) =>
    `${API_ROOT_V1}/threads/${encodeURIComponent(threadId)}/continue`,
  threadUpdateState: (threadId: string) =>
    `${API_ROOT_V1}/threads/${encodeURIComponent(threadId)}/update`,
  threadFork: (threadId: string) =>
    `${API_ROOT_V1}/threads/${encodeURIComponent(threadId)}/fork`,
} as const;
