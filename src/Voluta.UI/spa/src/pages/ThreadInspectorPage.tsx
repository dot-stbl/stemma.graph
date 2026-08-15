import {
  Button,
  makeStyles,
  MessageBar,
  MessageBarBody,
  Subtitle1,
  Subtitle2,
  Text,
  tokens,
} from "@fluentui/react-components";
import {
  ArrowLeft20Regular,
  Checkmark16Regular,
  ChevronDown20Regular,
  ChevronUp20Regular,
  Copy16Regular,
} from "@fluentui/react-icons";
import { Link, useParams } from "@tanstack/react-router";
import { useQueryClient } from "@tanstack/react-query";
import { useCallback, useEffect, useRef, useState } from "react";
import type { ThreadHistoryItem } from "@/api/types";
import { ActionsBar } from "@/components/ActionsBar";
import { HistoryScrubber } from "@/components/HistoryScrubber";
import { LoadingSkeleton } from "@/components/LoadingSkeleton";
import { StateViewer } from "@/components/StateViewer";
import { StatusPill } from "@/components/StatusPill";
import { StreamPanel } from "@/components/StreamPanel";
import { VolutaFlow } from "@/components/VolutaFlow";
import { hitlQueryKey } from "@/hooks/useHitl";
import { useThread, useThreadHistory, threadQueryKey } from "@/hooks/useThread";
import { threadsQueryKey } from "@/hooks/useThreads";
import { useTopology } from "@/hooks/useTopology";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalL,
  },
  headerRow: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalM,
    flexWrap: "wrap",
  },
  backLink: {
    display: "inline-flex",
    alignItems: "center",
    color: tokens.colorNeutralForeground2,
    textDecoration: "none",
    ":hover": {
      color: tokens.colorNeutralForeground1,
    },
  },
  titleBlock: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXXS,
    minWidth: 0,
  },
  idRow: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalXS,
    minWidth: 0,
  },
  threadId: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground2,
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  headerMeta: {
    marginLeft: "auto",
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
  },
  step: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    whiteSpace: "nowrap",
  },
  panel: {
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    padding: tokens.spacingVerticalM,
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
  },
  grid: {
    display: "grid",
    gridTemplateColumns: "1fr 1fr",
    gap: tokens.spacingHorizontalL,
    alignItems: "start",
    "@media (max-width: 1100px)": {
      gridTemplateColumns: "1fr",
    },
  },
  historyPreview: {
    margin: 0,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    whiteSpace: "pre-wrap",
    maxHeight: "200px",
    overflow: "auto",
  },
  panelHeader: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
  },
  legend: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
});

export function ThreadInspectorPage() {
  const styles = useStyles();
  const { threadId } = useParams({ from: "/threads/$threadId" });
  const thread = useThread(threadId);
  const history = useThreadHistory(threadId);
  const topology = useTopology();
  const queryClient = useQueryClient();
  const [historyFocus, setHistoryFocus] = useState<ThreadHistoryItem | null>(
    null,
  );
  const [copied, setCopied] = useState(false);
  const [graphOpen, setGraphOpen] = useState(true);

  // Live SSE events push checkpoint changes — refresh the affected queries
  // debounced so a burst of tokens does not hammer the API.
  const refreshTimer = useRef<number | null>(null);
  const onStreamEvent = useCallback(() => {
    if (refreshTimer.current !== null) {
      return;
    }
    refreshTimer.current = window.setTimeout(() => {
      refreshTimer.current = null;
      void queryClient.invalidateQueries({ queryKey: threadsQueryKey });
      void queryClient.invalidateQueries({ queryKey: threadQueryKey(threadId) });
      void queryClient.invalidateQueries({ queryKey: hitlQueryKey });
    }, 1_500);
  }, [queryClient, threadId]);

  useEffect(
    () => () => {
      if (refreshTimer.current !== null) {
        window.clearTimeout(refreshTimer.current);
      }
    },
    [],
  );

  useEffect(() => {
    if (!copied) {
      return;
    }
    const timer = setTimeout(() => setCopied(false), 1500);
    return () => clearTimeout(timer);
  }, [copied]);

  const interrupted =
    thread.data?.status?.toLowerCase() === "interrupted";

  const copyThreadId = () => {
    if (navigator.clipboard) {
      void navigator.clipboard.writeText(threadId);
    }
    setCopied(true);
  };

  return (
    <div className={styles.root}>
      <div className={styles.headerRow}>
        <Link to="/threads" className={styles.backLink} aria-label="Back to threads">
          <ArrowLeft20Regular />
        </Link>
        <div className={styles.titleBlock}>
          <Subtitle1 as="h1">Thread inspector</Subtitle1>
          <div className={styles.idRow}>
            <span className={styles.threadId} title={threadId}>
              {threadId}
            </span>
            <Button
              appearance="subtle"
              size="small"
              icon={
                copied ? <Checkmark16Regular /> : <Copy16Regular />
              }
              aria-label="Copy thread id"
              onClick={copyThreadId}
            />
          </div>
        </div>
        {thread.data && (
          <div className={styles.headerMeta}>
            <StatusPill status={thread.data.status} />
            <span className={styles.step}>step {thread.data.step}</span>
          </div>
        )}
      </div>

      <div className={styles.panel}>
        <Subtitle2 as="h2">Actions</Subtitle2>
        <ActionsBar threadId={threadId} interrupted={interrupted} />
      </div>

      {topology.data && (
        <div className={styles.panel}>
          <div className={styles.panelHeader}>
            <Subtitle2 as="h2">Graph</Subtitle2>
            <Button
              appearance="subtle"
              size="small"
              icon={graphOpen ? <ChevronUp20Regular /> : <ChevronDown20Regular />}
              onClick={() => setGraphOpen((open) => !open)}
              aria-label={graphOpen ? "Collapse graph" : "Expand graph"}
            >
              {graphOpen ? "Hide" : "Show"}
            </Button>
          </div>
          {graphOpen && (
            <>
              <VolutaFlow
                topology={topology.data}
                height={380}
                highlight={{
                  current: thread.data?.lastNode ?? null,
                  next: thread.data?.nextNodes ?? null,
                }}
              />
              <Text className={styles.legend}>
                brand pulse = current node · dashed = next · amber border =
                conditional
              </Text>
            </>
          )}
        </div>
      )}
      {topology.isLoading && (
        <div className={styles.panel}>
          <LoadingSkeleton variant="panel" rows={3} aria-label="Loading graph" />
        </div>
      )}

      {thread.isLoading && (
        <div className={styles.panel}>
          <LoadingSkeleton variant="panel" rows={2} aria-label="Loading checkpoint" />
        </div>
      )}
      {thread.isError && (
        <MessageBar intent="error">
          <MessageBarBody>
            {(thread.error as Error)?.message ?? "Failed to load thread"}
          </MessageBarBody>
        </MessageBar>
      )}

      {thread.data && (
        <div className={styles.grid}>
          <div className={styles.panel}>
            <Subtitle2 as="h2">State</Subtitle2>
            <StateViewer snapshot={thread.data} />
          </div>
          <div className={styles.panel}>
            <Subtitle2 as="h2">Live stream (SSE)</Subtitle2>
            <StreamPanel threadId={threadId} onEvent={onStreamEvent} />
          </div>
        </div>
      )}

      <div className={styles.panel}>
        <Subtitle2 as="h2">History</Subtitle2>
        <HistoryScrubber
          items={history.data}
          isLoading={history.isLoading}
          error={(history.error as Error) ?? null}
          onSelect={setHistoryFocus}
        />
        {historyFocus && (
          <pre className={styles.historyPreview}>
            {JSON.stringify(historyFocus, null, 2)}
          </pre>
        )}
      </div>
    </div>
  );
}
