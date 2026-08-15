import { useQuery } from "@tanstack/react-query";
import { api } from "@/api/client";

export const topologyQueryKey = ["topology"] as const;

export function useTopology() {
  return useQuery({
    queryKey: topologyQueryKey,
    queryFn: () => api.getTopology(),
    staleTime: 60_000,
  });
}
