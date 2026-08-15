import {
  makeStyles,
  mergeClasses,
  tokens,
  Tooltip,
} from "@fluentui/react-components";
import {
  BranchFork20Regular,
  List20Regular,
  PanelLeftContract20Regular,
  PanelLeftExpand20Regular,
  PersonFeedback20Regular,
  WeatherMoonRegular,
  WeatherSunnyRegular,
} from "@fluentui/react-icons";
import { Link, useRouterState } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import { CommandPalette } from "@/components/CommandPalette";
import { VolutaMark } from "@/components/VolutaMark";
import { useHitl } from "@/hooks/useHitl";
import { useTheme } from "@/theme/ThemeContext";

const RAIL_WIDE = 240;
const RAIL_COLLAPSED = 48;

const useStyles = makeStyles({
  root: {
    display: "grid",
    height: "100vh",
    position: "relative",
    backgroundColor: tokens.colorNeutralBackground2,
    color: tokens.colorNeutralForeground1,
  },
  rail: {
    gridRow: "1",
    position: "relative",
    borderRight: `1px solid ${tokens.colorNeutralStroke2}`,
    backgroundColor: tokens.colorNeutralBackground1,
    display: "flex",
    flexDirection: "column",
    padding: `${tokens.spacingVerticalM} ${tokens.spacingHorizontalS} ${tokens.spacingVerticalS}`,
    gap: tokens.spacingVerticalXXS,
    overflow: "hidden",
    minWidth: 0,
    userSelect: "none",
  },
  brand: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalXS} ${tokens.spacingHorizontalXS} ${tokens.spacingVerticalM}`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
    marginBottom: tokens.spacingVerticalS,
    whiteSpace: "nowrap",
  },
  brandCentered: {
    justifyContent: "center",
  },
  brandTitle: {
    fontWeight: tokens.fontWeightSemibold,
    fontSize: tokens.fontSizeBase300,
    color: tokens.colorNeutralForeground1,
    letterSpacing: "-0.01em",
    overflow: "hidden",
    textOverflow: "ellipsis",
  },
  railToggle: {
    position: "absolute",
    top: tokens.spacingVerticalS,
    zIndex: "20",
    width: "22px",
    height: "22px",
    cursor: "pointer",
    border: `1px solid ${tokens.colorNeutralStroke2}`,
    borderRadius: tokens.borderRadiusCircular,
    backgroundColor: tokens.colorNeutralBackground1,
    color: tokens.colorNeutralForeground3,
    display: "flex",
    alignItems: "center",
    justifyContent: "center",
    transitionProperty: "left",
    transitionDuration: "160ms",
    transitionTimingFunction: "cubic-bezier(0.33, 0, 0.67, 1)",
    ":hover": {
      color: tokens.colorNeutralForeground1,
      backgroundColor: tokens.colorNeutralBackground1Hover,
    },
    ":active": {
      backgroundColor: tokens.colorNeutralBackground1Pressed,
      transform: "scale(0.92)",
    },
  },
  nav: {
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXXS,
  },
  navLink: {
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderRadius: tokens.borderRadiusMedium,
    color: tokens.colorNeutralForeground2,
    textDecoration: "none",
    fontSize: tokens.fontSizeBase300,
    whiteSpace: "nowrap",
  },
  navLinkCollapsed: {
    justifyContent: "center",
    paddingInline: "0",
    position: "relative",
  },
  navActive: {
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorBrandForeground1,
    fontWeight: tokens.fontWeightSemibold,
  },
  navIcon: {
    flexShrink: 0,
  },
  navBadge: {
    marginLeft: "auto",
    flexShrink: 0,
    minWidth: "20px",
    height: "20px",
    paddingInline: tokens.spacingHorizontalXS,
    display: "inline-flex",
    alignItems: "center",
    justifyContent: "center",
    borderRadius: tokens.borderRadiusCircular,
    backgroundColor: tokens.colorPaletteMarigoldBackground2,
    color: tokens.colorPaletteMarigoldForeground2,
    fontSize: tokens.fontSizeBase100,
    fontWeight: tokens.fontWeightSemibold,
    lineHeight: 1,
  },
  navBadgeCollapsed: {
    position: "absolute",
    top: "2px",
    right: "2px",
    marginLeft: "0",
    minWidth: "16px",
    height: "16px",
    fontSize: "9px",
    paddingInline: "3px",
    border: `1.5px solid ${tokens.colorNeutralBackground1}`,
  },
  railFooter: {
    marginTop: "auto",
    paddingTop: tokens.spacingVerticalS,
    borderTop: `1px solid ${tokens.colorNeutralStroke2}`,
    display: "flex",
    flexDirection: "column",
    gap: tokens.spacingVerticalXXS,
  },
  footerBtn: {
    cursor: "pointer",
    border: "none",
    backgroundColor: "transparent",
    display: "flex",
    alignItems: "center",
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} ${tokens.spacingHorizontalM}`,
    borderRadius: tokens.borderRadiusMedium,
    color: tokens.colorNeutralForeground2,
    fontSize: tokens.fontSizeBase300,
    whiteSpace: "nowrap",
    ":hover": {
      backgroundColor: tokens.colorNeutralBackground1Hover,
      color: tokens.colorNeutralForeground1,
    },
  },
  footerBtnCollapsed: {
    justifyContent: "center",
    paddingInline: "0",
    height: "36px",
  },
  content: {
    overflow: "auto",
    padding: tokens.spacingVerticalL,
  },
});

const RAIL_STORAGE_KEY = "voluta-studio-rail";

export interface AppShellProps {
  children: ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  const styles = useStyles();
  const pathname = useRouterState({ select: (state) => state.location.pathname });
  const hitl = useHitl();
  const hitlCount = hitl.data?.length ?? 0;

  const { theme, toggleTheme } = useTheme();
  const isDark = theme === "dark";

  const [collapsed, setCollapsed] = useState(() => {
    if (typeof window === "undefined") {
      return false;
    }
    return window.localStorage.getItem(RAIL_STORAGE_KEY) === "collapsed";
  });

  const [paletteOpen, setPaletteOpen] = useState(false);

  useEffect(() => {
    window.localStorage.setItem(
      RAIL_STORAGE_KEY,
      collapsed ? "collapsed" : "expanded",
    );
  }, [collapsed]);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        setPaletteOpen((current) => !current);
      }
    };
    window.addEventListener("keydown", onKeyDown);
    return () => window.removeEventListener("keydown", onKeyDown);
  }, []);

  const isActive = (prefix: string) =>
    pathname === prefix || pathname.startsWith(`${prefix}/`);

  const railWidth = collapsed ? RAIL_COLLAPSED : RAIL_WIDE;

  const navLinkClass = (active: boolean) =>
    mergeClasses(
      styles.navLink,
      collapsed && styles.navLinkCollapsed,
      active && styles.navActive,
    );

  const badge = collapsed ? (
    <span
      className={mergeClasses(styles.navBadge, styles.navBadgeCollapsed)}
      aria-label={`${hitlCount} interrupted`}
    >
      {hitlCount > 9 ? "9+" : hitlCount}
    </span>
  ) : hitlCount > 0 ? (
    <span className={styles.navBadge} aria-label={`${hitlCount} interrupted`}>
      {hitlCount}
    </span>
  ) : null;

  return (
    <div
      className={styles.root}
      style={{
        gridTemplateColumns: `${railWidth}px 1fr`,
        transitionProperty: "grid-template-columns",
        transitionDuration: "160ms",
        transitionTimingFunction: "cubic-bezier(0.33, 0, 0.67, 1)",
      }}
    >
      <aside className={styles.rail} aria-label="Primary">
        <div
          className={mergeClasses(styles.brand, collapsed && styles.brandCentered)}
        >
          <VolutaMark />
          {!collapsed && <span className={styles.brandTitle}>Voluta Studio</span>}
        </div>

        <nav className={styles.nav}>
          {collapsed ? (
            <Tooltip content="Threads" relationship="label" showDelay={300}>
              <Link to="/threads" className={navLinkClass(isActive("/threads"))}>
                <List20Regular className={styles.navIcon} />
              </Link>
            </Tooltip>
          ) : (
            <Link to="/threads" className={navLinkClass(isActive("/threads"))}>
              <List20Regular className={styles.navIcon} />
              Threads
            </Link>
          )}
          {collapsed ? (
            <Tooltip
              content={`HITL (${hitlCount})`}
              relationship="label"
              showDelay={300}
            >
              <Link to="/hitl" className={navLinkClass(isActive("/hitl"))}>
                <PersonFeedback20Regular className={styles.navIcon} />
                {badge}
              </Link>
            </Tooltip>
          ) : (
            <Link to="/hitl" className={navLinkClass(isActive("/hitl"))}>
              <PersonFeedback20Regular className={styles.navIcon} />
              HITL
              {badge}
            </Link>
          )}
          {collapsed ? (
            <Tooltip content="Topology" relationship="label" showDelay={300}>
              <Link to="/topology" className={navLinkClass(isActive("/topology"))}>
                <BranchFork20Regular className={styles.navIcon} />
              </Link>
            </Tooltip>
          ) : (
            <Link to="/topology" className={navLinkClass(isActive("/topology"))}>
              <BranchFork20Regular className={styles.navIcon} />
              Topology
            </Link>
          )}
        </nav>

        <div className={styles.railFooter}>
          {collapsed ? (
            <Tooltip
              content={isDark ? "Light theme" : "Dark theme"}
              relationship="label"
              showDelay={300}
            >
              <button
                type="button"
                className={mergeClasses(
                  styles.footerBtn,
                  styles.footerBtnCollapsed,
                )}
                aria-label={isDark ? "Switch to light theme" : "Switch to dark theme"}
                onClick={toggleTheme}
              >
                {isDark ? <WeatherSunnyRegular /> : <WeatherMoonRegular />}
              </button>
            </Tooltip>
          ) : (
            <button
              type="button"
              className={styles.footerBtn}
              aria-label={isDark ? "Switch to light theme" : "Switch to dark theme"}
              onClick={toggleTheme}
            >
              {isDark ? <WeatherSunnyRegular /> : <WeatherMoonRegular />}
              {isDark ? "Light theme" : "Dark theme"}
            </button>
          )}
        </div>
      </aside>

      <Tooltip
        content={collapsed ? "Expand sidebar" : "Collapse sidebar"}
        relationship="label"
        showDelay={300}
      >
        <button
          type="button"
          className={styles.railToggle}
          style={{ left: `calc(${railWidth}px - 11px)` }}
          aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
          aria-expanded={!collapsed}
          onClick={() => setCollapsed((current) => !current)}
        >
          {collapsed ? (
            <PanelLeftExpand20Regular />
          ) : (
            <PanelLeftContract20Regular />
          )}
        </button>
      </Tooltip>

      <main className={styles.content}>{children}</main>

      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)} />
    </div>
  );
}
