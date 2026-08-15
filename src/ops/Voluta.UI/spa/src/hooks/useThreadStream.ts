import { useCallback, useEffect, useRef, useState } from "react";
import { api } from "@/api/client";
import type { StreamEventWire } from "@/api/types";

export type StreamMode = "checkpoint" | "resume" | "invoke";

export type StreamStatus =
  | "idle"
  | "connecting"
  | "live"
  | "reconnecting"
  | "offline";

const MAX_RETRIES = 3;
const BACKOFF_MS = [1_000, 2_000, 4_000] as const;

export interface StreamLine {
  id: number;
  at: string;
  kind: string;
  text: string;
  raw?: StreamEventWire;
}

export interface ThreadStreamOptions {
  /** Called for every received stream event (not meta lines). */
  onEvent?: (event: StreamEventWire) => void;
}

export function useThreadStream(
  threadId: string | undefined,
  options?: ThreadStreamOptions,
) {
  const [lines, setLines] = useState<StreamLine[]>([]);
  const [status, setStatus] = useState<StreamStatus>("idle");
  const [retryAttempt, setRetryAttempt] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const sourceRef = useRef<EventSource | null>(null);
  const timerRef = useRef<number | null>(null);
  const runIdRef = useRef(0);
  const seqRef = useRef(0);
  const retriesRef = useRef(0);
  const finishedRef = useRef(false);
  const openSourceRef = useRef<(mode: StreamMode) => void>(() => {});
  const onEventRef = useRef(options?.onEvent);

  useEffect(() => {
    onEventRef.current = options?.onEvent;
  }, [options?.onEvent]);

  const cancelTimers = useCallback(() => {
    if (timerRef.current !== null) {
      window.clearTimeout(timerRef.current);
      timerRef.current = null;
    }
  }, []);

  const closeSource = useCallback(() => {
    if (sourceRef.current) {
      sourceRef.current.close();
      sourceRef.current = null;
    }
  }, []);

  const stop = useCallback(() => {
    // Invalidate any pending reconnect from this run.
    runIdRef.current += 1;
    cancelTimers();
    closeSource();
    setStatus("idle");
  }, [cancelTimers, closeSource]);

  const clear = useCallback(() => {
    setLines([]);
    setError(null);
    seqRef.current = 0;
  }, []);

  const push = useCallback((kind: string, text: string, raw?: StreamEventWire) => {
    seqRef.current += 1;
    const at = new Date().toLocaleTimeString(undefined, { hour12: false });
    setLines((prev) => [
      ...prev,
      { id: seqRef.current, at, kind, text, raw },
    ]);
  }, []);

  const openSource = useCallback(
    (mode: StreamMode) => {
      if (!threadId) {
        setError("Select a thread first");
        return;
      }
      const runId = runIdRef.current;
      const source = new EventSource(api.streamUrl(threadId, mode));
      sourceRef.current = source;

      source.onopen = () => {
        if (runIdRef.current !== runId) {
          return;
        }
        retriesRef.current = 0;
        setRetryAttempt(0);
        setStatus("live");
        setError(null);
      };

      source.addEventListener("stream", (event) => {
        if (runIdRef.current !== runId) {
          return;
        }
        const message = event as MessageEvent<string>;
        try {
          const data = JSON.parse(message.data) as StreamEventWire;
          onEventRef.current?.(data);
          const kind = (data.kind ?? "event").toString();
          const nodes = (data.nodeNames ?? []).join(",");
          push(
            kind.toLowerCase().includes("interrupt") ? "interrupt" : kind.toLowerCase(),
            `step=${data.step ?? "—"}  ${kind}  [${nodes}]  ${data.payload ?? ""}`,
            data,
          );
        } catch {
          push("event", message.data);
        }
      });

      source.addEventListener("done", () => {
        if (runIdRef.current !== runId) {
          return;
        }
        finishedRef.current = true;
        push("done", "done");
        stop();
      });

      source.onerror = () => {
        if (runIdRef.current !== runId) {
          return;
        }
        closeSource();

        if (finishedRef.current) {
          setStatus("idle");
          return;
        }

        if (retriesRef.current < MAX_RETRIES) {
          const delay = BACKOFF_MS[retriesRef.current];
          retriesRef.current += 1;
          const attempt = retriesRef.current;
          setStatus("reconnecting");
          setRetryAttempt(attempt);
          push("meta", `connection lost — retry ${attempt}/${MAX_RETRIES} in ${delay / 1000}s`);
          timerRef.current = window.setTimeout(() => {
            timerRef.current = null;
            if (runIdRef.current !== runId) {
              return;
            }
            openSourceRef.current(mode);
          }, delay);
        } else {
          setStatus("offline");
          setError("Stream lost — retries exhausted");
          push("meta", `gave up after ${MAX_RETRIES} retries`);
          runIdRef.current += 1;
        }
      };
    },
    [threadId, push, stop, closeSource],
  );

  const start = useCallback(
    (mode: StreamMode = "checkpoint") => {
      if (!threadId) {
        setError("Select a thread first");
        return;
      }

      // Tear down any previous run before starting fresh.
      runIdRef.current += 1;
      cancelTimers();
      closeSource();
      setLines([]);
      setError(null);
      seqRef.current = 0;
      retriesRef.current = 0;
      setRetryAttempt(0);
      finishedRef.current = false;

      setStatus("connecting");
      push("meta", `connecting ${mode}…`);
      openSourceRef.current(mode);
    },
    [threadId, cancelTimers, closeSource, push],
  );

  // Keep the reconnect indirection pointing at the latest openSource.
  useEffect(() => {
    openSourceRef.current = openSource;
  }, [openSource]);

  useEffect(() => {
    return () => {
      stop();
    };
  }, [stop]);

  return { lines, status, retryAttempt, error, start, stop, clear };
}
