import {
  Badge,
  Button,
  makeStyles,
  MessageBar,
  MessageBarBody,
  Spinner,
  Text,
  tokens,
} from "@fluentui/react-components";
import { useMemo, useState } from "react";
import type { ThreadHistoryItem } from "@/api/types";
import {
  diffChannelValues,
  isDiffEmpty,
  truncateValue,
  type ValuesDiff,
} from "@/lib/historyDiff";
import { StatusPill } from "./StatusPill";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
  },
  hint: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  list: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
    maxHeight: "320px",
    overflow: "auto",
  },
  row: {
    display: "grid",
    gridTemplateColumns: "72px 120px 1fr auto",
    gap: tokens.spacingHorizontalS,
    alignItems: "center",
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalS}`,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    backgroundColor: tokens.colorNeutralBackground1,
    cursor: "pointer",
  },
  rowFocus: {
    border: `2px solid ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  rowCompare: {
    border: `1px dashed ${tokens.colorBrandStroke1}`,
    backgroundColor: tokens.colorBrandBackground2,
  },
  mono: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
  },
  muted: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
  },
  actions: {
    display: "flex",
    gap: tokens.spacingHorizontalXS,
  },
  diff: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
  },
  diffHeader: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    flexWrap: "wrap",
  },
  diffSectionTitle: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    textTransform: "uppercase",
    letterSpacing: "0.04em",
  },
  added: { color: tokens.colorPaletteGreenForeground1 },
  changed: { color: tokens.colorPaletteMarigoldForeground2 },
  removed: { color: tokens.colorPaletteRedForeground1 },
  diffRow: {
    display: "grid",
    gridTemplateColumns: "160px 1fr",
    gap: tokens.spacingHorizontalS,
    alignItems: "baseline",
    paddingBlock: tokens.spacingVerticalXXS,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    wordBreak: "break-all",
  },
  diffKey: {
    fontWeight: tokens.fontWeightSemibold,
    overflow: "hidden",
    textOverflow: "ellipsis",
  },
  diffValue: {
    color: tokens.colorNeutralForeground2,
    whiteSpace: "pre-wrap",
  },
});

export interface HistoryScrubberProps {
  items: ThreadHistoryItem[] | undefined;
  isLoading: boolean;
  error: Error | null;
  /** Called with the focused step (first click) or null on reset. */
  onSelect?: (item: ThreadHistoryItem | null) => void;
}

function DiffSection({
  title,
  tone,
  rows,
}: {
  title: string;
  tone: "added" | "changed" | "removed";
  rows: React.ReactNode;
}) {
  const styles = useStyles();
  return (
    <section>
      <span className={`${styles.diffSectionTitle} ${styles[tone]}`}>
        {title}
      </span>
      {rows}
    </section>
  );
}

export function HistoryScrubber({
  items,
  isLoading,
  error,
  onSelect,
}: HistoryScrubberProps) {
  const styles = useStyles();
  const [focusStep, setFocusStep] = useState<number | null>(null);
  const [compareStep, setCompareStep] = useState<number | null>(null);

  const sorted = useMemo(
    () => [...(items ?? [])].sort((a, b) => a.step - b.step),
    [items],
  );

  const focusItem = focusStep !== null
    ? (sorted.find((item) => item.step === focusStep) ?? null)
    : null;
  const compareItem = compareStep !== null
    ? (sorted.find((item) => item.step === compareStep) ?? null)
    : null;

  const diff: ValuesDiff | null = useMemo(() => {
    if (!focusItem || !compareItem) {
      return null;
    }
    const [earlier, later] =
      focusItem.step <= compareItem.step
        ? [focusItem, compareItem]
        : [compareItem, focusItem];
    return diffChannelValues(earlier.values, later.values);
  }, [focusItem, compareItem]);

  const handleRowClick = (step: number) => {
    if (compareStep !== null) {
      // Third click (or click after a pair is complete): reset to a fresh focus.
      setFocusStep(step);
      setCompareStep(null);
      onSelect?.(sorted.find((item) => item.step === step) ?? null);
      return;
    }
    if (focusStep === null) {
      setFocusStep(step);
      onSelect?.(sorted.find((item) => item.step === step) ?? null);
      return;
    }
    if (step === focusStep) {
      // Click the focused step again: reset.
      setFocusStep(null);
      setCompareStep(null);
      onSelect?.(null);
      return;
    }
    setCompareStep(step);
  };

  if (isLoading) {
    return <Spinner size="tiny" label="Loading history…" />;
  }

  if (error) {
    const message = error.message;
    const is501 =
      message.toLowerCase().includes("not supported") ||
      message.toLowerCase().includes("501") ||
      message.toLowerCase().includes("list_not_supported");

    return (
      <MessageBar intent={is501 ? "warning" : "error"}>
        <MessageBarBody>
          {is501
            ? "History list is not supported by this checkpoint store."
            : message}
        </MessageBarBody>
      </MessageBar>
    );
  }

  if (sorted.length === 0) {
    return <Text className={styles.muted}>No history steps.</Text>;
  }

  return (
    <div className={styles.root}>
      <Text className={styles.hint}>
        click a step to focus · click a second step to diff channels · click
        again to reset
      </Text>
      <div className={styles.list}>
        {sorted.map((item) => {
          const isFocus = item.step === focusStep;
          const isCompare = item.step === compareStep;
          const className = isFocus
            ? `${styles.row} ${styles.rowFocus}`
            : isCompare
              ? `${styles.row} ${styles.rowCompare}`
              : styles.row;
          return (
            <div
              key={`${item.threadId}-${item.step}`}
              className={className}
              role="button"
              tabIndex={0}
              aria-pressed={isFocus || isCompare}
              onClick={() => handleRowClick(item.step)}
              onKeyDown={(event) => {
                if (event.key === "Enter" || event.key === " ") {
                  event.preventDefault();
                  handleRowClick(item.step);
                }
              }}
            >
              <span className={styles.mono}>#{item.step}</span>
              <StatusPill status={item.status} />
              <span className={styles.mono}>{item.lastNode ?? "—"}</span>
              <div className={styles.actions}>
                <Button size="small" appearance="subtle" disabled
                  title="API #69 pending"
                >
                  Fork
                </Button>
              </div>
            </div>
          );
        })}
      </div>

      {diff && (
        <div className={styles.diff}>
          <div className={styles.diffHeader}>
            <Badge appearance="tint" color="informative" size="small">
              diff
            </Badge>
            <span className={styles.mono}>
              step {Math.min(focusStep ?? 0, compareStep ?? 0)} →{" "}
              {Math.max(focusStep ?? 0, compareStep ?? 0)} · channelValues
            </span>
            <Button
              size="small"
              appearance="subtle"
              onClick={() => {
                setCompareStep(null);
              }}
            >
              Close diff
            </Button>
          </div>

          {isDiffEmpty(diff) && (
            <Text className={styles.muted}>
              No channel changes between these steps.
            </Text>
          )}

          {diff.added.length > 0 && (
            <DiffSection
              title={`added (${diff.added.length})`}
              tone="added"
              rows={diff.added.map(({ key, value }) => (
                <div key={key} className={styles.diffRow}>
                  <span className={`${styles.diffKey} ${styles.added}`}>{key}</span>
                  <span className={styles.diffValue}>{truncateValue(value)}</span>
                </div>
              ))}
            />
          )}
          {diff.changed.length > 0 && (
            <DiffSection
              title={`changed (${diff.changed.length})`}
              tone="changed"
              rows={diff.changed.map(({ key, before, after }) => (
                <div key={key} className={styles.diffRow}>
                  <span className={`${styles.diffKey} ${styles.changed}`}>{key}</span>
                  <span className={styles.diffValue}>
                    {truncateValue(before)} → {truncateValue(after)}
                  </span>
                </div>
              ))}
            />
          )}
          {diff.removed.length > 0 && (
            <DiffSection
              title={`removed (${diff.removed.length})`}
              tone="removed"
              rows={diff.removed.map(({ key, value }) => (
                <div key={key} className={styles.diffRow}>
                  <span className={`${styles.diffKey} ${styles.removed}`}>{key}</span>
                  <span className={styles.diffValue}>{truncateValue(value)}</span>
                </div>
              ))}
            />
          )}
        </div>
      )}
    </div>
  );
}
