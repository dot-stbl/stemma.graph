# Voluta Studio (SPA)

Fluent 2 light ops console for Voluta. Lives inside `Voluta.UI` so production
builds embed under `wwwroot/studio` for NuGet packaging.

## Stack

- bun + Vite 6 + React 19 + TypeScript strict
- `@fluentui/react-components` (webLightTheme)
- `@tanstack/react-query` + `@tanstack/react-router`

## Run (local)

```
# Terminal 1
dotnet run --project samples/UiHost
# → http://localhost:5188/voluta  (legacy shell)

# Terminal 2
cd src/Voluta.UI/spa
bun install
bun run dev
# → http://localhost:3847
```

Vite proxies `/voluta` → `http://localhost:5188`. Leave `VITE_API_BASE_URL`
empty (same origin) or set it to `http://localhost:5188` in `.env.development`
if you skip the proxy.

## Scripts

| Script | Purpose |
|--------|---------|
| `bun run dev` | Vite dev server on port **3847** |
| `bun run typecheck` | `tsc -b` (strict) |
| `bun run build` | typecheck + emit to `../wwwroot/studio` |
| `bun run preview` | preview production build |

## Build output

```ts
// vite.config.ts
build: {
  outDir: '../wwwroot/studio',
  emptyOutDir: true,
}
base: './'  // assets resolve under any host path prefix
```

After `bun run build`, static files land in `src/Voluta.UI/wwwroot/studio/`.

## API surface (existing host)

All calls use prefix `/voluta/api`:

| Method | Path |
|--------|------|
| GET | `/topology` |
| GET | `/hitl` |
| GET | `/threads` |
| GET | `/threads/{id}` |
| GET | `/threads/{id}/history` |
| POST | `/threads/{id}/resume` |
| GET | `/threads/{id}/stream?mode=checkpoint\|resume\|invoke` (SSE) |

Continue / UpdateState / Fork actions are UI stubs until API **#69**.
