import { Badge, makeStyles, tokens } from "@fluentui/react-components";
import type { ThreadStatus } from "@/api/types";
import { appearanceFor } from "@/lib/statusAppearance";

const useStyles = makeStyles({
  pill: {
    fontSize: tokens.fontSizeBase200,
    fontWeight: tokens.fontWeightSemibold,
    textTransform: "uppercase",
    letterSpacing: "0.02em",
  },
});

export interface StatusPillProps {
  status: ThreadStatus | null | undefined;
}

export function StatusPill({ status }: StatusPillProps) {
  const styles = useStyles();
  const label = status?.trim() ? status : "—";

  return (
    <Badge
      className={styles.pill}
      appearance="tint"
      color={appearanceFor(label)}
      size="small"
    >
      {label}
    </Badge>
  );
}
