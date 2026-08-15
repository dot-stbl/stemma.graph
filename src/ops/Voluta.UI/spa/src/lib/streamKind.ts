export type StreamTone = "green" | "marigold" | "red" | "neutral";

/**
 * Map a stream event kind to a display tone.
 * messages → green, interrupt → marigold, error → red, everything else neutral.
 * "done" stays green (terminal completion of the stream).
 */
export function streamKindTone(kind: string): StreamTone {
  const normalized = kind.toLowerCase();
  if (normalized.includes("error") || normalized.includes("fail")) {
    return "red";
  }
  if (normalized.includes("interrupt")) {
    return "marigold";
  }
  if (normalized.includes("message") || normalized === "done") {
    return "green";
  }
  return "neutral";
}
