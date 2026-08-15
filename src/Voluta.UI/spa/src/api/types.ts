/** Wire types matching host JSON (camelCase via System.Text.Json Web defaults). */

export type ThreadStatus =
  | "Done"
  | "Interrupted"
  | "Failed"
  | "Cancelled"
  | "Running"
  | string;

export interface ThreadSummary {
  threadId: string;
  status: ThreadStatus;
  step: number;
  lastNode?: string | null;
  goal?: string | null;
}

export interface HitlThreadSummary {
  threadId: string;
  step: number;
  lastNode?: string | null;
  interruptPayload?: string | null;
}

export interface PendingInterrupt {
  taskId?: string | null;
  nodeName?: string | null;
  payload?: string | null;
  taskPayload?: string | null;
}

export interface PendingWrite {
  taskId?: string | null;
  channelName?: string | null;
  value?: string | null;
}

export interface PendingSend {
  nodeName?: string | null;
  taskId?: string | null;
  payload?: string | null;
}

export interface CheckpointSnapshot {
  formatVersion?: number;
  threadId: string;
  step: number;
  status: ThreadStatus;
  lastNode?: string | null;
  nextNodes?: string[] | null;
  interruptPayload?: string | null;
  pendingInterrupts?: PendingInterrupt[] | null;
  channelValues?: Record<string, string> | null;
  channelVersions?: Record<string, number | string> | null;
  versionsSeen?: Record<string, unknown> | null;
  pendingWrites?: PendingWrite[] | null;
  pendingSends?: PendingSend[] | null;
}

/** History item from GET .../history (ThreadSnapshot wire). */
export interface ThreadHistoryItem {
  threadId: string;
  step: number;
  status: ThreadStatus;
  lastNode?: string | null;
  nextNodes?: string[] | null;
  interruptPayload?: string | null;
  pendingInterrupts?: PendingInterrupt[] | null;
  values?: Record<string, string> | null;
}

export interface TopologyEdge {
  source: string;
  target: string;
}

export interface TopologyDescription {
  nodes: string[];
  channels: Record<string, string>;
  staticEdges: TopologyEdge[];
  conditionalSources?: string[] | null;
  recursionLimit?: number | null;
}

export interface ResumeRequest {
  kind?: string | null;
  payload?: string | null;
}

export interface ResumeTerminal {
  kind: string;
  step: number;
  payload?: string | null;
  nodeNames?: string[] | null;
}

export interface StreamEventWire {
  mode?: string;
  kind?: string;
  step?: number;
  nodeNames?: string[];
  writes?: Array<{ channelName?: string; value?: string }>;
  state?: Record<string, string>;
  payload?: string | null;
}

export interface ApiErrorBody {
  error?: string;
  code?: string;
  title?: string;
  detail?: string;
}

/** /api/v1 studio contract (MapStudioApi) — verified against host. */

/** ContinueInvoke — no request body. */
export type ContinueRequest = Record<string, never>;

/** One channel write on the studio wire (value is free-form JSON). */
export interface StudioChannelWrite {
  channelName: string;
  value?: unknown;
}

/** UpdateState — POST /api/v1/threads/{id}/update { writes: [...] }. */
export interface UpdateStateRequest {
  writes?: StudioChannelWrite[] | null;
}

/** Fork — POST /api/v1/threads/{id}/fork { newThreadId, step? }. */
export interface ForkRequest {
  newThreadId?: string | null;
  step?: number | null;
}

/** ResumeTerminal wire (same shape as legacy resume). */
export interface StudioMutationResult {
  kind: string;
  step: number;
  payload?: string | null;
  nodeNames?: string[] | null;
}
