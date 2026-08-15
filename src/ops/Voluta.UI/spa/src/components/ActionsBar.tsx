import {
  Button,
  Dialog,
  DialogActions,
  DialogBody,
  DialogContent,
  DialogSurface,
  DialogTitle,
  Field,
  Input,
  makeStyles,
  MessageBar,
  MessageBarBody,
  Spinner,
  Textarea,
  tokens,
  Tooltip,
} from "@fluentui/react-components";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "@/api/client";
import { hitlQueryKey, useResumeThread } from "@/hooks/useHitl";
import { threadsQueryKey } from "@/hooks/useThreads";
import { threadHistoryQueryKey, threadQueryKey } from "@/hooks/useThread";
import type { StudioMutationResult } from "@/api/types";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexWrap: "wrap",
    gap: tokens.spacingHorizontalS,
    alignItems: "center",
  },
  feedback: {
    width: "100%",
  },
  dialogTextarea: {
    minWidth: "360px",
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
  },
});

function useStudioMutation(
  threadId: string | undefined,
  mutationFn: (id: string) => Promise<StudioMutationResult>,
) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      if (!threadId) {
        throw new Error("threadId is missing");
      }
      return mutationFn(threadId);
    },
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: threadsQueryKey }),
        queryClient.invalidateQueries({
          queryKey: threadId ? threadQueryKey(threadId) : [],
        }),
        queryClient.invalidateQueries({
          queryKey: threadId ? threadHistoryQueryKey(threadId) : [],
        }),
        queryClient.invalidateQueries({ queryKey: hitlQueryKey }),
      ]);
    },
  });
}

export interface ActionsBarProps {
  threadId: string | undefined;
  interrupted?: boolean;
}

export function ActionsBar({ threadId, interrupted }: ActionsBarProps) {
  const styles = useStyles();
  const resume = useResumeThread();

  const [updateOpen, setUpdateOpen] = useState(false);
  const [forkOpen, setForkOpen] = useState(false);
  const [updateChannel, setUpdateChannel] = useState("");
  const [updateValue, setUpdateValue] = useState("");
  const [forkId, setForkId] = useState("");
  const [forkStep, setForkStep] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const continueMutation = useStudioMutation(threadId, (id) =>
    api.continueThread(id),
  );
  const updateMutation = useStudioMutation(threadId, (id) =>
    api.updateThreadState(id, {
      writes: [
        {
          channelName: updateChannel.trim(),
          value: tryParseJson(updateValue),
        },
      ],
    }),
  );
  const forkMutation = useStudioMutation(threadId, (id) =>
    api.forkThread(id, {
      newThreadId: forkId.trim(),
      step: forkStep.trim() === "" ? null : Number(forkStep),
    }),
  );

  const anyPending =
    resume.isPending ||
    continueMutation.isPending ||
    updateMutation.isPending ||
    forkMutation.isPending;
  const disabled = !threadId || anyPending;

  const submitUpdate = () => {
    if (!updateChannel.trim()) {
      setFormError("Channel name is required");
      return;
    }
    setFormError(null);
    updateMutation.mutate(undefined, {
      onSuccess: () => setUpdateOpen(false),
    });
  };

  const submitFork = () => {
    if (!forkId.trim()) {
      setFormError("New thread id is required");
      return;
    }
    setFormError(null);
    forkMutation.mutate(undefined, {
      onSuccess: () => setForkOpen(false),
    });
  };

  return (
    <div className={styles.root}>
      <Tooltip
        content={
          interrupted
            ? "Approve the interrupt and resume execution"
            : "Thread is not interrupted — nothing to resume"
        }
        relationship="label"
      >
        <span>
          <Button
            appearance="primary"
            size="small"
            disabled={disabled || !interrupted}
            onClick={() => {
              if (!threadId) {
                return;
              }
              resume.mutate({ threadId, body: { kind: "approve" } });
            }}
          >
            Approve & resume
          </Button>
        </span>
      </Tooltip>
      <Tooltip
        content={
          interrupted
            ? "Reject the interrupt"
            : "Thread is not interrupted — nothing to reject"
        }
        relationship="label"
      >
        <span>
          <Button
            appearance="secondary"
            size="small"
            disabled={disabled || !interrupted}
            onClick={() => {
              if (!threadId) {
                return;
              }
              resume.mutate({ threadId, body: { kind: "reject" } });
            }}
          >
            Reject
          </Button>
        </span>
      </Tooltip>

      <Tooltip content="Continue invocation from the current state" relationship="label">
        <span>
          <Button
            appearance="subtle"
            size="small"
            disabled={disabled}
            onClick={() => continueMutation.mutate(undefined)}
          >
            Continue
          </Button>
        </span>
      </Tooltip>
      <Tooltip content="Write a channel value into the thread state" relationship="label">
        <span>
          <Button
            appearance="subtle"
            size="small"
            disabled={disabled}
            onClick={() => {
              setUpdateChannel("");
              setUpdateValue("");
              setFormError(null);
              setUpdateOpen(true);
            }}
          >
            Update state
          </Button>
        </span>
      </Tooltip>
      <Tooltip content="Fork the thread to a new id at a given step" relationship="label">
        <span>
          <Button
            appearance="subtle"
            size="small"
            disabled={disabled}
            onClick={() => {
              setForkId(threadId ? `${threadId}-fork` : "");
              setForkStep("");
              setFormError(null);
              setForkOpen(true);
            }}
          >
            Fork
          </Button>
        </span>
      </Tooltip>

      <Dialog open={updateOpen} onOpenChange={(_e, data) => setUpdateOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Update state</DialogTitle>
            <DialogContent>
              <Field label="Channel name" required>
                <Input
                  value={updateChannel}
                  onChange={(_e, data) => setUpdateChannel(data.value)}
                  placeholder="messages"
                />
              </Field>
              <Field
                label="Value (JSON)"
                hint="Raw JSON — invalid JSON is sent as a string"
                style={{ marginTop: tokens.spacingVerticalS }}
              >
                <Textarea
                  className={styles.dialogTextarea}
                  value={updateValue}
                  onChange={(_e, data) => setUpdateValue(data.value)}
                  placeholder='{ "role": "user", "content": "…" }'
                  rows={5}
                />
              </Field>
              {formError && (
                <MessageBar intent="error" style={{ marginTop: tokens.spacingVerticalS }}>
                  <MessageBarBody>{formError}</MessageBarBody>
                </MessageBar>
              )}
              {updateMutation.isError && (
                <MessageBar intent="error" style={{ marginTop: tokens.spacingVerticalS }}>
                  <MessageBarBody>
                    {(updateMutation.error as Error)?.message ?? "Update failed"}
                  </MessageBarBody>
                </MessageBar>
              )}
            </DialogContent>
            <DialogActions>
              {updateMutation.isPending && <Spinner size="tiny" />}
              <Button appearance="secondary" onClick={() => setUpdateOpen(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={submitUpdate} disabled={anyPending}>
                Update
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      <Dialog open={forkOpen} onOpenChange={(_e, data) => setForkOpen(data.open)}>
        <DialogSurface>
          <DialogBody>
            <DialogTitle>Fork thread</DialogTitle>
            <DialogContent>
              <Field label="New thread id" required>
                <Input
                  value={forkId}
                  onChange={(_e, data) => setForkId(data.value)}
                />
              </Field>
              <Field
                label="At step (optional)"
                hint="Empty = latest checkpoint"
                style={{ marginTop: tokens.spacingVerticalS }}
              >
                <Input
                  value={forkStep}
                  onChange={(_e, data) => setForkStep(data.value)}
                  type="number"
                  placeholder="4"
                />
              </Field>
              {formError && (
                <MessageBar intent="error" style={{ marginTop: tokens.spacingVerticalS }}>
                  <MessageBarBody>{formError}</MessageBarBody>
                </MessageBar>
              )}
              {forkMutation.isError && (
                <MessageBar intent="error" style={{ marginTop: tokens.spacingVerticalS }}>
                  <MessageBarBody>
                    {(forkMutation.error as Error)?.message ?? "Fork failed"}
                  </MessageBarBody>
                </MessageBar>
              )}
            </DialogContent>
            <DialogActions>
              {forkMutation.isPending && <Spinner size="tiny" />}
              <Button appearance="secondary" onClick={() => setForkOpen(false)}>
                Cancel
              </Button>
              <Button appearance="primary" onClick={submitFork} disabled={anyPending}>
                Fork
              </Button>
            </DialogActions>
          </DialogBody>
        </DialogSurface>
      </Dialog>

      {resume.isError && (
        <MessageBar intent="error" className={styles.feedback}>
          <MessageBarBody>
            {(resume.error as Error)?.message ?? "Resume failed"}
          </MessageBarBody>
        </MessageBar>
      )}
      {resume.isSuccess && (
        <MessageBar intent="success" className={styles.feedback}>
          <MessageBarBody>
            Resumed — terminal: {resume.data.kind} @ step {resume.data.step}
          </MessageBarBody>
        </MessageBar>
      )}
      {continueMutation.isSuccess && (
        <MessageBar intent="success" className={styles.feedback}>
          <MessageBarBody>
            Continued — terminal: {continueMutation.data.kind} @ step{" "}
            {continueMutation.data.step}
          </MessageBarBody>
        </MessageBar>
      )}
      {continueMutation.isError && (
        <MessageBar intent="error" className={styles.feedback}>
          <MessageBarBody>
            {(continueMutation.error as Error)?.message ?? "Continue failed"}
          </MessageBarBody>
        </MessageBar>
      )}
      {updateMutation.isSuccess && (
        <MessageBar intent="success" className={styles.feedback}>
          <MessageBarBody>
            State updated — ok @ step {updateMutation.data.step}
          </MessageBarBody>
        </MessageBar>
      )}
      {forkMutation.isSuccess && (
        <MessageBar intent="success" className={styles.feedback}>
          <MessageBarBody>
            Forked — terminal: {forkMutation.data.kind} @ step{" "}
            {forkMutation.data.step}
          </MessageBarBody>
        </MessageBar>
      )}
      {forkMutation.isError && (
        <MessageBar intent="error" className={styles.feedback}>
          <MessageBarBody>
            {(forkMutation.error as Error)?.message ?? "Fork failed"}
          </MessageBarBody>
        </MessageBar>
      )}
    </div>
  );
}

function tryParseJson(raw: string): unknown {
  const trimmed = raw.trim();
  if (trimmed === "") {
    return null;
  }
  try {
    return JSON.parse(trimmed) as unknown;
  } catch {
    return raw;
  }
}
