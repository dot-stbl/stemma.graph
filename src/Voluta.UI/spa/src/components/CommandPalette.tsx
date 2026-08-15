import {
  makeStyles,
  mergeClasses,
  tokens,
} from "@fluentui/react-components";
import {
  ArrowRight16Regular,
  BranchFork20Regular,
  List20Regular,
  PersonFeedback20Regular,
  Search20Regular,
} from "@fluentui/react-icons";
import { Link, useNavigate } from "@tanstack/react-router";
import { useEffect, useMemo, useRef, useState } from "react";
import { useThreads } from "@/hooks/useThreads";

const useStyles = makeStyles({
  overlay: {
    position: "fixed",
    inset: 0,
    zIndex: "1000",
    display: "flex",
    alignItems: "flex-start",
    justifyContent: "center",
    paddingTop: "12vh",
    backgroundColor: "rgba(0, 0, 0, 0.35)",
  },
  panel: {
    width: "min(560px, calc(100vw - 48px))",
    backgroundColor: tokens.colorNeutralBackground1,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusLarge,
    boxShadow:
      "0 8px 24px rgba(0,0,0,.14), 0 2px 6px rgba(0,0,0,.12)",
    overflow: "hidden",
    display: "flex",
    flexDirection: "column",
  },
  inputRow: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  input: {
    flex: 1,
    fontSize: tokens.fontSizeBase400,
    color: tokens.colorNeutralForeground1,
    backgroundColor: "transparent",
    border: "none",
    outline: "none",
    padding: "0",
    margin: "0",
    fontFamily: "inherit",
    "::placeholder": {
      color: tokens.colorNeutralForeground3,
    },
  },
  list: {
    maxHeight: "320px",
    overflowY: "auto",
    padding: tokens.spacingVerticalXS,
  },
  sectionLabel: {
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
    fontSize: tokens.fontSizeBase100,
    textTransform: "uppercase",
    letterSpacing: "0.06em",
    color: tokens.colorNeutralForeground3,
  },
  item: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderRadius: tokens.borderRadiusMedium,
    color: tokens.colorNeutralForeground1,
    textDecoration: "none",
    cursor: "pointer",
    fontSize: tokens.fontSizeBase300,
  },
  itemActive: {
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground1,
  },
  itemIcon: {
    flexShrink: 0,
    color: tokens.colorNeutralForeground2,
  },
  itemLabel: {
    overflow: "hidden",
    textOverflow: "ellipsis",
    whiteSpace: "nowrap",
  },
  itemHint: {
    marginLeft: "auto",
    flexShrink: 0,
    fontSize: tokens.fontSizeBase200,
    color: tokens.colorNeutralForeground3,
    fontFamily: tokens.fontFamilyMonospace,
  },
  empty: {
    padding: `${tokens.spacingVerticalL} ${tokens.spacingHorizontalM}`,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase300,
    textAlign: "center",
  },
  footer: {
    display: "flex",
    gap: tokens.spacingHorizontalL,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalM}`,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    color: tokens.colorNeutralForeground3,
    fontSize: tokens.fontSizeBase100,
  },
  kbd: {
    fontFamily: tokens.fontFamilyMonospace,
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusSmall,
    padding: "0 4px",
  },
});

interface PaletteItem {
  id: string;
  label: string;
  hint: string;
  icon: React.ReactNode;
  to: string;
}

interface CommandPaletteProps {
  open: boolean;
  onClose: () => void;
}

export function CommandPalette({ open, onClose }: CommandPaletteProps) {
  if (!open) {
    return null;
  }
  return <PaletteInner onClose={onClose} />;
}

function PaletteInner({ onClose }: { onClose: () => void }) {
  const styles = useStyles();
  const navigate = useNavigate();
  const threads = useThreads();
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const listRef = useRef<HTMLDivElement | null>(null);
  const inputRef = useRef<HTMLInputElement | null>(null);

  useEffect(() => {
    inputRef.current?.focus();
  }, [query]);

  const commands = useMemo<PaletteItem[]>(
    () => [
      {
        id: "nav-threads",
        label: "Threads",
        hint: "page",
        icon: <List20Regular className={styles.itemIcon} />,
        to: "/threads",
      },
      {
        id: "nav-hitl",
        label: "HITL queue",
        hint: "page",
        icon: <PersonFeedback20Regular className={styles.itemIcon} />,
        to: "/hitl",
      },
      {
        id: "nav-topology",
        label: "Topology",
        hint: "page",
        icon: <BranchFork20Regular className={styles.itemIcon} />,
        to: "/topology",
      },
    ],
    [styles.itemIcon],
  );

  const threadItems = useMemo<PaletteItem[]>(
    () =>
      (threads.data ?? []).map((thread) => ({
        id: `thread-${thread.threadId}`,
        label: thread.threadId,
        hint: thread.status,
        icon: <ArrowRight16Regular className={styles.itemIcon} />,
        to: `/threads/${thread.threadId}`,
      })),
    [threads.data, styles.itemIcon],
  );

  const needle = query.trim().toLowerCase();
  const filteredCommands = needle
    ? commands.filter((command) => command.label.toLowerCase().includes(needle))
    : commands;
  const filteredThreads = needle
    ? threadItems.filter((thread) => thread.label.toLowerCase().includes(needle))
    : threadItems;

  const flat = useMemo(
    () => [...filteredCommands, ...filteredThreads],
    [filteredCommands, filteredThreads],
  );

  // Derived clamp — activeIndex can exceed the list after filtering.
  const safeIndex = Math.min(activeIndex, Math.max(flat.length - 1, 0));

  const commit = (item: PaletteItem | undefined) => {
    if (!item) {
      return;
    }
    onClose();
    void navigate({ to: item.to });
  };

  const onKeyDown = (event: React.KeyboardEvent<HTMLDivElement>) => {
    if (event.key === "Escape") {
      event.preventDefault();
      onClose();
      return;
    }
    if (event.key === "ArrowDown") {
      event.preventDefault();
      setActiveIndex((current) => (current + 1) % Math.max(flat.length, 1));
      return;
    }
    if (event.key === "ArrowUp") {
      event.preventDefault();
      setActiveIndex(
        (current) =>
          (current - 1 + Math.max(flat.length, 1)) % Math.max(flat.length, 1),
      );
      return;
    }
    if (event.key === "Enter") {
      event.preventDefault();
      commit(flat[safeIndex]);
    }
  };

  const renderItem = (item: PaletteItem, index: number) => (
    <Link
      key={item.id}
      to={item.to}
      className={mergeClasses(
        styles.item,
        index === safeIndex && styles.itemActive,
      )}
      onMouseEnter={() => setActiveIndex(index)}
      onClick={(event) => {
        event.preventDefault();
        commit(item);
      }}
    >
      {item.icon}
      <span className={styles.itemLabel}>{item.label}</span>
      <span className={styles.itemHint}>{item.hint}</span>
    </Link>
  );

  return (
    <div
      className={styles.overlay}
      onClick={onClose}
      role="presentation"
    >
      <div
        className={styles.panel}
        onClick={(event) => event.stopPropagation()}
        onKeyDown={onKeyDown}
        role="dialog"
        aria-modal="true"
        aria-label="Command palette"
      >
        <div className={styles.inputRow}>
          <Search20Regular />
          <input
            ref={inputRef}
            className={styles.input}
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search threads, pages…"
            aria-label="Search threads and pages"
            spellCheck={false}
          />
        </div>
        <div className={styles.list} ref={listRef}>
          {filteredCommands.length > 0 && (
            <div className={styles.sectionLabel}>Go to</div>
          )}
          {filteredCommands.map((item, index) => renderItem(item, index))}
          {filteredThreads.length > 0 && (
            <div className={styles.sectionLabel}>Threads</div>
          )}
          {filteredThreads.map((item, index) =>
            renderItem(item, filteredCommands.length + index),
          )}
          {flat.length === 0 && (
            <div className={styles.empty}>No matches for “{query}”</div>
          )}
        </div>
        <div className={styles.footer}>
          <span>
            <span className={styles.kbd}>↑↓</span> navigate
          </span>
          <span>
            <span className={styles.kbd}>enter</span> open
          </span>
          <span>
            <span className={styles.kbd}>esc</span> close
          </span>
        </div>
      </div>
    </div>
  );
}
