import {
  CheckmarkCircle20Regular,
} from "@fluentui/react-icons";
import {
  makeStyles,
  MessageBar,
  MessageBarBody,
  Subtitle1,
  Text,
  tokens,
} from "@fluentui/react-components";
import { HitlCard } from "@/components/HitlCard";
import { LoadingSkeleton } from "@/components/LoadingSkeleton";
import { useHitl } from "@/hooks/useHitl";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalM,
  },
  list: {
    display: "grid",
    gridTemplateColumns: "repeat(auto-fill, minmax(320px, 1fr))",
    gap: tokens.spacingHorizontalM,
  },
  muted: {
    color: tokens.colorNeutralForeground3,
  },
  empty: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    gap: tokens.spacingVerticalS,
    minHeight: "240px",
    color: tokens.colorNeutralForeground3,
  },
  emptyIcon: {
    fontSize: "28px",
    color: tokens.colorPaletteGreenForeground1,
  },
  emptyTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground2,
  },
  emptyHint: {
    fontSize: tokens.fontSizeBase200,
  },
});

export function HitlPage() {
  const styles = useStyles();
  const { data, isLoading, isError, error } = useHitl();

  return (
    <div className={styles.root}>
      <Subtitle1 as="h1">HITL queue</Subtitle1>
      <Text size={200} className={styles.muted}>
        Interrupted threads awaiting approve / reject via the resume API.
      </Text>

      {isLoading && (
        <LoadingSkeleton variant="cards" rows={4} aria-label="Loading HITL queue" />
      )}
      {isError && (
        <MessageBar intent="error">
          <MessageBarBody>
            {(error as Error)?.message ?? "Failed to load HITL queue"}
          </MessageBarBody>
        </MessageBar>
      )}
      {!isLoading && !isError && (data?.length ?? 0) === 0 && (
        <div className={styles.empty}>
          <CheckmarkCircle20Regular className={styles.emptyIcon} />
          <span className={styles.emptyTitle}>Queue is empty</span>
          <span className={styles.emptyHint}>
            Threads that pause for approval appear here.
          </span>
        </div>
      )}
      <div className={styles.list}>
        {(data ?? []).map((item) => (
          <HitlCard key={item.threadId} item={item} />
        ))}
      </div>
    </div>
  );
}
