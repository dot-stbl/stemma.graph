import {
  Badge,
  Button,
  Dropdown,
  makeStyles,
  Option,
  tokens,
} from "@fluentui/react-components";
import { ArrowDown16Regular } from "@fluentui/react-icons";
import { useEffect, useRef, useState, type UIEvent } from "react";
import type { StreamEventWire } from "@/api/types";
import {
  useThreadStream,
  type StreamMode,
  type StreamStatus,
} from "@/hooks/useThreadStream";
import { streamKindTone, type StreamTone } from "@/lib/streamKind";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
    minHeight: "220px",
  },
  toolbar: {
    display: "flex",
    gap: tokens.spacingHorizontalS,
    alignItems: "center",
    flexWrap: "wrap",
  },
  count: {
    marginLeft: "auto",
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    whiteSpace: "nowrap",
  },
  logWrap: {
    position: "relative",
  },
  log: {
    flex: 1,
    minHeight: "180px",
    maxHeight: "320px",
    overflow: "auto",
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    lineHeight: tokens.lineHeightBase200,
  },
  line: {
    display: "grid",
    gridTemplateColumns: "64px 1fr",
    gap: tokens.spacingHorizontalS,
    paddingBlock: "2px",
  },
  ts: {
    color: tokens.colorNeutralForeground3,
  },
  toneGreen: {
    color: tokens.colorPaletteGreenForeground1,
  },
  toneMarigold: {
    color: tokens.colorPaletteMarigoldForeground2,
  },
  toneRed: {
    color: tokens.colorPaletteRedForeground1,
  },
  toneNeutral: {
    color: tokens.colorNeutralForeground2,
  },
  meta: {
    color: tokens.colorNeutralForeground3,
  },
  error: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
  },
  newEvents: {
    position: "absolute",
    bottom: tokens.spacingVerticalS,
    right: tokens.spacingHorizontalM,
  },
});

const TONE_CLASS: Record<
  StreamTone,
  "toneGreen" | "toneMarigold" | "toneRed" | "toneNeutral"
> = {
  green: "toneGreen",
  marigold: "toneMarigold",
  red: "toneRed",
  neutral: "toneNeutral",
};

function StatusChip({ status, retryAttempt }: { status: StreamStatus; retryAttempt: number }) {
  if (status === "idle") {
    return null;
  }
  const label =
    status === "reconnecting"
      ? `reconnecting ${retryAttempt}/3`
      : status;
  const color =
    status === "live"
      ? "success"
      : status === "reconnecting"
        ? "warning"
        : status === "offline"
          ? "danger"
          : "informative";
  return (
    <Badge appearance="filled" color={color} size="small">
      {label}
    </Badge>
  );
}

export interface StreamPanelProps {
  threadId: string | undefined;
  onEvent?: (event: StreamEventWire) => void;
}

export function StreamPanel({ threadId, onEvent }: StreamPanelProps) {
  const styles = useStyles();
  const { lines, status, retryAttempt, error, start, stop, clear } =
    useThreadStream(threadId, { onEvent });
  const [mode, setMode] = useState<StreamMode>("checkpoint");
  const logRef = useRef<HTMLDivElement | null>(null);
  const [atBottom, setAtBottom] = useState(true);
  const [seenCount, setSeenCount] = useState(0);

  const connected =
    status === "connecting" || status === "live" || status === "reconnecting";

  const pendingEvents = atBottom
    ? 0
    : Math.max(0, lines.length - seenCount);

  const handleScroll = (event: UIEvent<HTMLDivElement>) => {
    const node = event.currentTarget;
    const distance = node.scrollHeight - node.scrollTop - node.clientHeight;
    const nowAtBottom = distance < 24;
    setAtBottom(nowAtBottom);
    if (nowAtBottom) {
      setSeenCount(lines.length);
    }
  };

  useEffect(() => {
    if (!atBottom) {
      return;
    }
    const node = logRef.current;
    if (node) {
      node.scrollTop = node.scrollHeight;
    }
  }, [lines, atBottom]);

  const jumpToBottom = () => {
    const node = logRef.current;
    if (node) {
      node.scrollTop = node.scrollHeight;
    }
    setSeenCount(lines.length);
    setAtBottom(true);
  };

  return (
    <div className={styles.root}>
      <div className={styles.toolbar}>
        <Dropdown
          aria-label="Stream mode"
          placeholder="mode"
          size="small"
          value={mode}
          selectedOptions={[mode]}
          disabled={connected}
          onOptionSelect={(_event, data) =>
            setMode((data.optionValue as StreamMode) ?? "checkpoint")
          }
          style={{ minWidth: 130 }}
        >
          <Option value="checkpoint">checkpoint</Option>
          <Option value="resume">resume</Option>
          <Option value="invoke">invoke</Option>
        </Dropdown>
        <Button
          appearance="primary"
          size="small"
          disabled={!threadId || connected}
          onClick={() => start(mode)}
        >
          Start stream
        </Button>
        <Button
          appearance="secondary"
          size="small"
          disabled={!connected}
          onClick={stop}
        >
          Stop
        </Button>
        <Button appearance="subtle" size="small" onClick={clear}>
          Clear
        </Button>
        <StatusChip status={status} retryAttempt={retryAttempt} />
        {lines.length > 0 && (
          <span className={styles.count}>{lines.length} events</span>
        )}
      </div>

      {error && <span className={styles.error}>{error}</span>}

      <div className={styles.logWrap}>
        <div className={styles.log} ref={logRef} onScroll={handleScroll}>
          {lines.length === 0 ? (
            <span className={styles.meta}>No events yet.</span>
          ) : (
            lines.map((line) => {
              const toneClass =
                line.kind === "meta"
                  ? styles.meta
                  : styles[TONE_CLASS[streamKindTone(line.kind)]];
              return (
                <div key={line.id} className={`${styles.line} ${toneClass}`}>
                  <span className={styles.ts}>{line.at}</span>
                  <span>{line.text}</span>
                </div>
              );
            })
          )}
        </div>
        {!atBottom && pendingEvents > 0 && (
          <Button
            className={styles.newEvents}
            size="small"
            appearance="primary"
            icon={<ArrowDown16Regular />}
            onClick={jumpToBottom}
          >
            {pendingEvents} new {pendingEvents === 1 ? "event" : "events"}
          </Button>
        )}
      </div>
    </div>
  );
}
