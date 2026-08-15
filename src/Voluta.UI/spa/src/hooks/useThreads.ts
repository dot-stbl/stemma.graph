import { useQuery } from "@tanstack/react-query";
import { api } from "@/api/client";

export const threadsQueryKey = ["threads"] as const;

export function useThreads() {
  return useQuery({
    queryKey: threadsQueryKey,
    queryFn: () => api.listThreads(),
    refetchInterval: 15_000,
  });
}
