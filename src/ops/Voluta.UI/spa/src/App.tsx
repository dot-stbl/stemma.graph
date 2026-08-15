import {
  createRootRoute,
  createRoute,
  createRouter,
  Navigate,
  Outlet,
  RouterProvider,
} from "@tanstack/react-router";
import { lazy, Suspense } from "react";
import { Spinner } from "@fluentui/react-components";
import { AppShell } from "@/layout/AppShell";
import { HitlPage } from "@/pages/HitlPage";

// Code-split: inspector + topology carry React Flow / dagre (~heavy);
// they load on first navigation instead of the initial bundle.
const ThreadsPage = lazy(() =>
  import("@/pages/ThreadsPage").then((module) => ({ default: module.ThreadsPage })),
);
const ThreadInspectorPage = lazy(() =>
  import("@/pages/ThreadInspectorPage").then((module) => ({
    default: module.ThreadInspectorPage,
  })),
);
const TopologyPage = lazy(() =>
  import("@/pages/TopologyPage").then((module) => ({
    default: module.TopologyPage,
  })),
);

function PageFallback() {
  return <Spinner size="small" label="Loading…" />;
}

const rootRoute = createRootRoute({
  component: () => (
    <AppShell>
      <Outlet />
    </AppShell>
  ),
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: () => <Navigate to="/threads" />,
});

const threadsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/threads",
  validateSearch: (search: Record<string, unknown>): { q?: string; status?: string } => ({
    q: typeof search.q === "string" ? search.q : undefined,
    status: typeof search.status === "string" ? search.status : undefined,
  }),
  component: () => (
    <Suspense fallback={<PageFallback />}>
      <ThreadsPage />
    </Suspense>
  ),
});

const threadRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/threads/$threadId",
  component: () => (
    <Suspense fallback={<PageFallback />}>
      <ThreadInspectorPage />
    </Suspense>
  ),
});

const hitlRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/hitl",
  component: HitlPage,
});

const topologyRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/topology",
  component: () => (
    <Suspense fallback={<PageFallback />}>
      <TopologyPage />
    </Suspense>
  ),
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  threadsRoute,
  threadRoute,
  hitlRoute,
  topologyRoute,
]);

const router = createRouter({
  routeTree,
  defaultPreload: "intent",
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}

export function App() {
  return <RouterProvider router={router} />;
}
