import {
  Accordion,
  AccordionHeader,
  AccordionItem,
  AccordionPanel,
  makeStyles,
  Text,
  tokens,
  Caption1,
} from "@fluentui/react-components";
import type { CheckpointSnapshot } from "@/api/types";
import { StatusPill } from "./StatusPill";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalM,
  },
  head: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    flexWrap: "wrap",
  },
  mono: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
  },
  grid: {
    display: "grid",
    gridTemplateColumns: "90px 1fr",
    gap: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
    fontSize: tokens.fontSizeBase200,
  },
  label: {
    color: tokens.colorNeutralForeground3,
  },
  pre: {
    margin: 0,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground3,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    lineHeight: tokens.lineHeightBase200,
    overflow: "auto",
    maxHeight: "420px",
    whiteSpace: "pre-wrap",
    wordBreak: "break-word",
  },
});

export interface StateViewerProps {
  snapshot: CheckpointSnapshot;
}

export function StateViewer({ snapshot }: StateViewerProps) {
  const styles = useStyles();
  const channels = snapshot.channelValues ?? {};
  const versions = snapshot.channelVersions ?? {};
  const interruptCount = snapshot.pendingInterrupts?.length ?? 0;

  return (
    <div className={styles.root}>
      <div className={styles.head}>
        <StatusPill status={snapshot.status} />
        <Text className={styles.mono}>step {snapshot.step}</Text>
        <Caption1 className={styles.mono}>{snapshot.threadId}</Caption1>
      </div>

      <div className={styles.grid}>
        <span className={styles.label}>last</span>
        <span className={styles.mono}>{snapshot.lastNode ?? "—"}</span>
        <span className={styles.label}>next</span>
        <span className={styles.mono}>
          {(snapshot.nextNodes ?? []).join(", ") || "—"}
        </span>
        <span className={styles.label}>interrupt</span>
        <span
          className={styles.mono}
          title={snapshot.interruptPayload ?? undefined}
        >
          {snapshot.interruptPayload ?? "—"}
        </span>
        <span className={styles.label}>versions</span>
        <span className={styles.mono}>
          {Object.entries(versions)
            .map(([key, value]) => `${key}@${value}`)
            .join(", ") || "—"}
        </span>
      </div>

      <Accordion collapsible multiple defaultOpenItems={["channels"]}>
        {interruptCount > 0 && (
          <AccordionItem value="interrupts">
            <AccordionHeader size="small">
              Pending interrupts ({interruptCount})
            </AccordionHeader>
            <AccordionPanel>
              <pre className={styles.pre}>
                {JSON.stringify(snapshot.pendingInterrupts, null, 2)}
              </pre>
            </AccordionPanel>
          </AccordionItem>
        )}
        <AccordionItem value="channels">
          <AccordionHeader size="small">
            Channel values ({Object.keys(channels).length})
          </AccordionHeader>
          <AccordionPanel>
            <pre className={styles.pre}>
              {Object.keys(channels).length === 0
                ? "/* empty */"
                : JSON.stringify(channels, null, 2)}
            </pre>
          </AccordionPanel>
        </AccordionItem>
        <AccordionItem value="raw">
          <AccordionHeader size="small">Full checkpoint JSON</AccordionHeader>
          <AccordionPanel>
            <pre className={styles.pre}>{JSON.stringify(snapshot, null, 2)}</pre>
          </AccordionPanel>
        </AccordionItem>
      </Accordion>
    </div>
  );
}
