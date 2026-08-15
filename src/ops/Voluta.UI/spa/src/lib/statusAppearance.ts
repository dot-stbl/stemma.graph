import type { ThreadStatus } from "@/api/types";

export type StatusAppearance =
  | "success"
  | "warning"
  | "danger"
  | "informative"
  | "subtle";

export function appearanceFor(status: ThreadStatus): StatusAppearance {
  const normalized = status.toLowerCase();
  if (normalized === "done") {
    return "success";
  }
  if (normalized === "interrupted") {
    return "warning";
  }
  if (normalized === "failed" || normalized === "error") {
    return "danger";
  }
  if (normalized === "running") {
    return "informative";
  }
  return "subtle";
}
