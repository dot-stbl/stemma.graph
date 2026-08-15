import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { api } from "@/api/client";
import type { ResumeRequest } from "@/api/types";
import { threadsQueryKey } from "./useThreads";
import { threadHistoryQueryKey, threadQueryKey } from "./useThread";

export const hitlQueryKey = ["hitl"] as const;

export function useHitl() {
  return useQuery({
    queryKey: hitlQueryKey,
    queryFn: () => api.listHitl(),
    refetchInterval: 10_000,
  });
}

export function useResumeThread() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({
      threadId,
      body,
    }: {
      threadId: string;
      body?: ResumeRequest;
    }) => api.resumeThread(threadId, body ?? {}),
    onSuccess: async (_data, variables) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: hitlQueryKey }),
        queryClient.invalidateQueries({ queryKey: threadsQueryKey }),
        queryClient.invalidateQueries({ queryKey: threadQueryKey(variables.threadId) }),
        queryClient.invalidateQueries({
          queryKey: threadHistoryQueryKey(variables.threadId),
        }),
      ]);
    },
  });
}
