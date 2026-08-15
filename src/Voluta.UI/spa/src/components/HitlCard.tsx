import {
  Badge,
  Button,
  Card,
  CardFooter,
  CardHeader,
  makeStyles,
  Text,
  tokens,
} from "@fluentui/react-components";
import { Link } from "@tanstack/react-router";
import type { HitlThreadSummary } from "@/api/types";
import { useResumeThread } from "@/hooks/useHitl";

function prettyPayload(raw: string): string {
  try {
    const parsed: unknown = JSON.parse(raw);
    return JSON.stringify(parsed, null, 2);
  } catch {
    return raw;
  }
}

const useStyles = makeStyles({
  card: {
    width: "100%",
  },
  headerRow: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    minWidth: 0,
  },
  mono: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
  },
  payload: {
    marginTop: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground3,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    whiteSpace: "pre-wrap",
    wordBreak: "break-word",
    maxHeight: "220px",
    overflow: "auto",
  },
  footer: {
    display: "flex",
    gap: tokens.spacingHorizontalS,
    alignItems: "center",
    flexWrap: "wrap",
  },
  link: {
    color: tokens.colorBrandForeground1,
    textDecoration: "none",
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
    ":hover": {
      textDecoration: "underline",
    },
  },
  errorText: {
    color: tokens.colorPaletteRedForeground1,
    fontSize: tokens.fontSizeBase200,
  },
});

export interface HitlCardProps {
  item: HitlThreadSummary;
}

export function HitlCard({ item }: HitlCardProps) {
  const styles = useStyles();
  const resume = useResumeThread();

  return (
    <Card className={styles.card} size="small">
      <CardHeader
        header={
          <div className={styles.headerRow}>
            <Link
              to="/threads/$threadId"
              params={{ threadId: item.threadId }}
              className={styles.link}
              title={item.threadId}
            >
              {item.threadId}
            </Link>
            <Badge appearance="tint" color="warning" size="small">
              interrupted
            </Badge>
          </div>
        }
        description={
          <Text className={styles.mono} size={200}>
            step {item.step}
            {item.lastNode ? ` · ${item.lastNode}` : ""}
          </Text>
        }
      />
      {item.interruptPayload && (
        <pre className={styles.payload}>
          {prettyPayload(item.interruptPayload)}
        </pre>
      )}
      <CardFooter className={styles.footer}>
        <Button
          appearance="primary"
          size="small"
          disabled={resume.isPending}
          onClick={() =>
            resume.mutate({ threadId: item.threadId, body: { kind: "approve" } })
          }
        >
          Approve
        </Button>
        <Button
          appearance="secondary"
          size="small"
          disabled={resume.isPending}
          onClick={() =>
            resume.mutate({ threadId: item.threadId, body: { kind: "reject" } })
          }
        >
          Reject
        </Button>
        {resume.isError && (
          <Text size={200} className={styles.errorText}>
            {(resume.error as Error).message}
          </Text>
        )}
      </CardFooter>
    </Card>
  );
}
