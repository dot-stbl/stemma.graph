import {
  Button,
  Dropdown,
  Input,
  makeStyles,
  MessageBar,
  MessageBarBody,
  Option,
  Subtitle1,
  tokens,
} from "@fluentui/react-components";
import {
  ArrowClockwise20Regular,
  List20Regular,
  Search20Regular,
} from "@fluentui/react-icons";
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { LoadingSkeleton } from "@/components/LoadingSkeleton";
import { ThreadTable } from "@/components/ThreadTable";
import { useThreads } from "@/hooks/useThreads";

const useStyles = makeStyles({
  root: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalM,
  },
  toolbar: {
    display: "flex",
    gap: tokens.spacingHorizontalS,
    flexWrap: "wrap",
    alignItems: "center",
  },
  toolbarEnd: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    marginLeft: "auto",
  },
  count: {
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase200,
    whiteSpace: "nowrap",
  },
  panel: {
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusMedium,
    overflow: "hidden",
  },
  loading: {
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    minHeight: "160px",
  },
  empty: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    justifyContent: "center",
    gap: tokens.spacingVerticalS,
    minHeight: "220px",
    color: tokens.colorNeutralForeground3,
  },
  emptyIcon: {
    fontSize: "28px",
  },
  emptyTitle: {
    fontSize: tokens.fontSizeBase300,
    fontWeight: tokens.fontWeightSemibold,
    color: tokens.colorNeutralForeground2,
  },
  emptyHint: {
    fontSize: tokens.fontSizeBase200,
  },
  search: {
    minWidth: "240px",
  },
});

export function ThreadsPage() {
  const styles = useStyles();
  const { data, isLoading, isError, error, isFetching, refetch } = useThreads();
  const { q: qParam, status: statusParam } = useSearch({ from: "/threads" });
  const navigate = useNavigate({ from: "/threads" });
  // Initialize from URL params on mount (refresh/shareable links);
  // afterwards the local state leads and is synced to the URL below.
  const [statusFilter, setStatusFilter] = useState<string>(statusParam ?? "all");
  const [search, setSearch] = useState(qParam ?? "");

  // State → URL (debounced, replace-only so typing does not spam history).
  useEffect(() => {
    const timer = setTimeout(() => {
      void navigate({
        search: () => ({
          q: search.trim() === "" ? undefined : search,
          status: statusFilter === "all" ? undefined : statusFilter,
        }),
        replace: true,
      });
    }, 250);
    return () => clearTimeout(timer);
  }, [search, statusFilter, navigate]);

  const statuses = useMemo(() => {
    const set = new Set<string>();
    for (const item of data ?? []) {
      set.add(item.status);
    }
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  }, [data]);

  const filtered = useMemo(() => {
    const needle = search.trim().toLowerCase();
    return (data ?? []).filter((item) => {
      if (statusFilter !== "all" && item.status !== statusFilter) {
        return false;
      }
      if (
        needle &&
        !item.threadId.toLowerCase().includes(needle) &&
        !(item.goal ?? "").toLowerCase().includes(needle) &&
        !(item.lastNode ?? "").toLowerCase().includes(needle)
      ) {
        return false;
      }
      return true;
    });
  }, [data, statusFilter, search]);

  const total = data?.length ?? 0;

  return (
    <div className={styles.root}>
      <Subtitle1 as="h1">Threads</Subtitle1>
      <div className={styles.toolbar}>
        <Input
          className={styles.search}
          aria-label="Search threads"
          placeholder="Search id, node, goal"
          value={search}
          onChange={(_event, data) => setSearch(data.value)}
          size="small"
          contentBefore={<Search20Regular />}
        />
        <Dropdown
          aria-label="Filter by status"
          placeholder="Status"
          selectedOptions={[statusFilter]}
          value={statusFilter === "all" ? "All statuses" : statusFilter}
          onOptionSelect={(_event, data) =>
            setStatusFilter(data.optionValue ?? "all")
          }
          style={{ minWidth: 160 }}
          size="small"
        >
          <Option value="all">All statuses</Option>
          {statuses.map((status) => (
            <Option key={status} value={status}>
              {status}
            </Option>
          ))}
        </Dropdown>
        <div className={styles.toolbarEnd}>
          {total > 0 && (
            <span className={styles.count}>
              {filtered.length} of {total}
            </span>
          )}
          <Button
            appearance="subtle"
            size="small"
            icon={<ArrowClockwise20Regular />}
            disabled={isFetching}
            onClick={() => {
              void refetch();
            }}
            aria-label="Refresh threads"
          >
            Refresh
          </Button>
        </div>
      </div>

      <div className={styles.panel}>
        {isLoading && (
          <LoadingSkeleton variant="table" rows={6} aria-label="Loading threads" />
        )}
        {isError && (
          <MessageBar intent="error">
            <MessageBarBody>
              {(error as Error)?.message ?? "Failed to load threads"}
            </MessageBarBody>
          </MessageBar>
        )}
        {!isLoading && !isError && total === 0 && (
          <div className={styles.empty}>
            <List20Regular className={styles.emptyIcon} />
            <span className={styles.emptyTitle}>No threads yet</span>
            <span className={styles.emptyHint}>
              Start a run — threads appear here as they execute.
            </span>
          </div>
        )}
        {!isLoading && !isError && total > 0 && <ThreadTable items={filtered} />}
      </div>
    </div>
  );
}
