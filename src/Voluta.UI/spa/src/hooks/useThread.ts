import { useQuery } from "@tanstack/react-query";
import { api } from "@/api/client";

export function threadQueryKey(threadId: string) {
  return ["thread", threadId] as const;
}

export function threadHistoryQueryKey(threadId: string) {
  return ["thread-history", threadId] as const;
}

export function useThread(threadId: string | undefined) {
  return useQuery({
    queryKey: threadQueryKey(threadId ?? ""),
    queryFn: () => api.getThread(threadId!),
    enabled: Boolean(threadId),
  });
}

export function useThreadHistory(threadId: string | undefined) {
  return useQuery({
    queryKey: threadHistoryQueryKey(threadId ?? ""),
    queryFn: () => api.getHistory(threadId!),
    enabled: Boolean(threadId),
    retry: false,
  });
}
