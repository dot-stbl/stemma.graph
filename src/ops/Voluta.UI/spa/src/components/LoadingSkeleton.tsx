import {
  makeStyles,
  mergeClasses,
  Skeleton,
  SkeletonItem,
  tokens,
} from "@fluentui/react-components";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXS,
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalM}`,
  },
  row: {
    display: "grid",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
  },
  rowTable: {
    gridTemplateColumns: "220px 90px 60px 140px 1fr",
  },
  rowCard: {
    gridTemplateColumns: "1fr",
  },
  panel: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalS,
    padding: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
  },
});

export type SkeletonVariant = "table" | "cards" | "panel";

export interface LoadingSkeletonProps {
  variant?: SkeletonVariant;
  rows?: number;
  "aria-label"?: string;
}

export function LoadingSkeleton({
  variant = "table",
  rows = 5,
  "aria-label": ariaLabel = "Loading",
}: LoadingSkeletonProps) {
  const styles = useStyles();
  const count = Math.max(1, rows);

  return (
    <Skeleton
      aria-label={ariaLabel}
      className={variant === "panel" ? styles.panel : styles.root}
    >
      {Array.from({ length: count }, (_, index) => (
        <div
          key={index}
          className={mergeClasses(
            styles.row,
            variant === "table" ? styles.rowTable : styles.rowCard,
          )}
        >
          <SkeletonItem size={16} />
          {variant === "table" && (
            <>
              <SkeletonItem size={16} />
              <SkeletonItem size={16} />
              <SkeletonItem size={16} />
              <SkeletonItem size={16} />
            </>
          )}
        </div>
      ))}
    </Skeleton>
  );
}
