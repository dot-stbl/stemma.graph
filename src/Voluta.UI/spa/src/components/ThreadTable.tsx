import {
  makeStyles,
  mergeClasses,
  Table,
  TableBody,
  TableCell,
  TableHeader,
  TableHeaderCell,
  TableRow,
  tokens,
} from "@fluentui/react-components";
import {
  ArrowSortDownRegular,
  ArrowSortUpRegular,
} from "@fluentui/react-icons";
import { Link } from "@tanstack/react-router";
import {
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnDef,
  type SortingState,
} from "@tanstack/react-table";
import { useState } from "react";
import type { ThreadSummary } from "@/api/types";
import { StatusPill } from "./StatusPill";

const useStyles = makeStyles({
  root: {
    width: "100%",
  },
  headerCell: {
    cursor: "pointer",
    userSelect: "none",
    ":hover": {
      color: tokens.colorNeutralForeground1,
    },
  },
  headerContent: {
    display: "inline-flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalXS,
  },
  sortIcon: {
    fontSize: "12px",
    color: tokens.colorNeutralForeground3,
  },
  sortIconActive: {
    color: tokens.colorBrandForeground1,
  },
  row: {
    ":hover": {
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
  },
  stepCell: {
    textAlign: "right",
  },
  mono: {
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
  },
  link: {
    color: tokens.colorBrandForeground1,
    textDecoration: "none",
    fontFamily: tokens.fontFamilyMonospace,
    fontSize: tokens.fontSizeBase200,
    display: "inline-block",
    maxWidth: "280px",
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
    verticalAlign: "bottom",
    ":hover": {
      textDecoration: "underline",
    },
  },
  goal: {
    color: tokens.colorNeutralForeground2,
    display: "inline-block",
    maxWidth: "360px",
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
    verticalAlign: "bottom",
  },
  empty: {
    padding: tokens.spacingVerticalXXXL,
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    color: tokens.colorNeutralForeground3,
  },
});

function ThreadIdCell({ item }: { item: ThreadSummary }) {
  const styles = useStyles();
  return (
    <Link
      to="/threads/$threadId"
      params={{ threadId: item.threadId }}
      className={styles.link}
      title={item.threadId}
    >
      {item.threadId}
    </Link>
  );
}

function StepCell({ item }: { item: ThreadSummary }) {
  const styles = useStyles();
  return <span className={styles.mono}>{item.step}</span>;
}

function LastNodeCell({ item }: { item: ThreadSummary }) {
  const styles = useStyles();
  return <span className={styles.mono}>{item.lastNode ?? "—"}</span>;
}

function GoalCell({ item }: { item: ThreadSummary }) {
  const styles = useStyles();
  return (
    <span className={styles.goal} title={item.goal ?? undefined}>
      {item.goal ?? "—"}
    </span>
  );
}

const columns: ColumnDef<ThreadSummary, unknown>[] = [
  {
    id: "threadId",
    accessorKey: "threadId",
    header: "Thread",
    cell: (info) => <ThreadIdCell item={info.row.original} />,
  },
  {
    id: "status",
    accessorKey: "status",
    header: "Status",
    cell: (info) => <StatusPill status={info.row.original.status} />,
  },
  {
    id: "step",
    accessorKey: "step",
    header: "Step",
    cell: (info) => <StepCell item={info.row.original} />,
  },
  {
    id: "lastNode",
    accessorFn: (row) => row.lastNode ?? "",
    header: "Last node",
    cell: (info) => <LastNodeCell item={info.row.original} />,
  },
  {
    id: "goal",
    accessorFn: (row) => row.goal ?? "",
    header: "Goal",
    cell: (info) => <GoalCell item={info.row.original} />,
  },
];

export interface ThreadTableProps {
  items: ThreadSummary[];
}

export function ThreadTable({ items }: ThreadTableProps) {
  const styles = useStyles();
  const [sorting, setSorting] = useState<SortingState>([
    { id: "threadId", desc: false },
  ]);

  // TanStack Table v8 intentionally returns unstable function references;
  // React Compiler memoization is not used in this app, so the
  // incompatible-library warning is a known false positive here.
  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data: items,
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  if (items.length === 0) {
    return (
      <div className={styles.empty}>No threads match the current filters.</div>
    );
  }

  return (
    <Table className={styles.root} size="small" aria-label="Threads">
      <TableHeader>
        <TableRow>
          {table.getHeaderGroups()[0]?.headers.map((header) => {
            const sorted = header.column.getIsSorted();
            return (
              <TableHeaderCell
                key={header.id}
                className={styles.headerCell}
                onClick={header.column.getToggleSortingHandler()}
                aria-sort={
                  sorted === "asc"
                    ? "ascending"
                    : sorted === "desc"
                      ? "descending"
                      : "none"
                }
              >
                <span className={styles.headerContent}>
                  {flexRender(
                    header.column.columnDef.header,
                    header.getContext(),
                  )}
                  {sorted === "asc" && (
                    <ArrowSortUpRegular
                      className={mergeClasses(
                        styles.sortIcon,
                        styles.sortIconActive,
                      )}
                    />
                  )}
                  {sorted === "desc" && (
                    <ArrowSortDownRegular
                      className={mergeClasses(
                        styles.sortIcon,
                        styles.sortIconActive,
                      )}
                    />
                  )}
                </span>
              </TableHeaderCell>
            );
          })}
        </TableRow>
      </TableHeader>
      <TableBody>
        {table.getRowModel().rows.map((row) => (
          <TableRow key={row.id} className={styles.row}>
            {row.getVisibleCells().map((cell) => (
              <TableCell
                key={cell.id}
                className={cell.column.id === "step" ? styles.stepCell : undefined}
              >
                {flexRender(cell.column.columnDef.cell, cell.getContext())}
              </TableCell>
            ))}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
