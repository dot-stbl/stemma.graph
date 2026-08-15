# UiHost — Voluta ops console sample

Minimal `WebApplication` that hosts **Voluta.UI** (Razor Class Library shell +
JSON API + SSE live stream) with a multi-node demo graph and seeded threads.

## Run

```bash
dotnet run --project samples/UiHost
```

Open:

| Surface | URL |
|---------|-----|
| Legacy shell (ops console) | http://localhost:5188/voluta |
| Studio SPA | http://localhost:5188/voluta/studio |
| Studio API v1 (HTTP/SSE) | http://localhost:5188/api/v1/* |
| API (legacy, unchanged) | http://localhost:5188/voluta/api/* |

Port is fixed in `Properties/launchSettings.json`. Do not leave `dotnet run`
running in agent sessions (agent-runtime-safety).

### Studio SPA (optional dist)

Studio is a Vite/React app under `src/Voluta.UI/spa`. Build embeds into
`src/Voluta.UI/wwwroot/studio/` (package `EmbeddedResource`):

```bash
cd src/Voluta.UI/spa
bun install
bun run build
dotnet build src/Voluta.UI
```

If dist is missing, `/voluta/studio` returns **503** HTML with build
instructions — the host and legacy shell still work.

**FE dev** (Vite on 3847, proxies `/voluta` → UiHost 5188):

```bash
# terminal 1 — host
dotnet run --project samples/UiHost
# terminal 2 — SPA
cd src/Voluta.UI/spa && bun run dev
# open http://localhost:3847
```

## What starts up

1. Compiles a multi-node graph with an in-memory checkpointer:

   ```
   START → intake → plan → retrieve → risk_gate ──(status==blocked)──► END
                                         └──(else)──► synthesize → notify → END
   ```

2. Seeds four threads so the console is not empty:

   | Thread | Outcome |
   |--------|---------|
   | `payment-hitl` | Interrupts at `risk_gate` (payment dual-control) |
   | `deploy-hitl` | Interrupts at `risk_gate` (prod deploy) |
   | `research-done` | Completes without HITL (`docs:` goal auto-approves) |
   | `audit-blocked` | Interrupts, then resume `reject` → `status=blocked` |

## Screens

| Tab | What |
|-----|------|
| Run inspector | Load checkpoint by thread id; optional SSE stream |
| HITL queue | Interrupted threads → Approve / Reject / SSE resume |
| Topology | `CompiledGraph.Describe()` nodes, edges, channels |

## Host wiring

```csharp
var session = new VolutaUiSession(graph, checkpointer);
builder.Services.AddVolutaUI(session);
app.MapGet("/", () => Results.Redirect("/voluta"));
app.MapVolutaUI(options => options.PathPrefix = "/voluta");

// Studio API v1 — same session via DI (AddVolutaUI registered it).
var studioApiOptions = new StudioApiOptions();
builder.Configuration.GetSection(StudioApiOptions.SectionName).Bind(studioApiOptions);
app.MapStudioApi(studioApiOptions);
```

## Studio API v1 (`/api/v1`, `MapStudioApi`)

Versioned SPA-oriented contract. Auth is **off by default**; when `StudioApi:ApiKey`
is set, every route requires `X-Api-Key: {key}` or `Authorization: Bearer {key}`
(fixed-time compare; 401 `studio.unauthorized` otherwise).

| Method | Path | Body / params | Behavior |
|--------|------|---------------|----------|
| GET | `/api/v1/topology` | — | Graph description (nodes, channels, edges) |
| GET | `/api/v1/threads` | — | Thread list (tracked + durable discovery) |
| GET | `/api/v1/threads/{id}` | — | Latest host state; 404 unknown thread |
| GET | `/api/v1/threads/{id}/history` | — | Steps oldest-first; 501 when checkpointer lacks list |
| POST | `/api/v1/threads/{id}/resume` | `{ kind, payload }` | `approve` (default) / `reject` / `update` → terminal event; 400 `studio.invalid_command` on unknown kind |
| POST | `/api/v1/threads/{id}/continue` | — | Continue a `Running` thread → terminal event |
| POST | `/api/v1/threads/{id}/update` | `{ writes: [{ channelName, value }] }` | Merge channel writes (reducer-aware); 400 on empty writes |
| POST | `/api/v1/threads/{id}/fork` | `{ step, newThreadId }` | Copy a history step onto a new thread; 400 on missing `newThreadId` |
| GET | `/api/v1/threads/{id}/stream` | SSE | `StreamEvent` frames (see modes below) |
| GET | `/api/v1/hitl` | — | Interrupted threads queue |

Studio stream (`/api/v1/threads/{id}/stream`) query params:

- `mode=checkpoint` (default) — one synthetic event from the latest checkpoint
  (`&auto=1` on an `Interrupted` thread resumes it with `kind`/`payload`)
- `mode=resume&kind=approve&payload=ok` — live resume stream
- `mode=continue` — live continue stream
- `mode=invoke&seed=…` — live invoke stream

## Routes (under `/voluta`, legacy)

| Method | Path | Role |
|--------|------|------|
| GET | `/` | Legacy UI shell |
| GET | `/studio`, `/studio/` | Studio SPA index (or 503 if not built) |
| GET | `/studio/*` | Studio assets / SPA fallback |
| GET | `/api/topology` | Graph description |
| GET | `/api/hitl` | Interrupted threads |
| GET | `/api/threads` | Thread list |
| GET | `/api/threads/{id}` | Checkpoint |
| GET | `/api/threads/{id}/history` | History |
| POST | `/api/threads/{id}/resume` | Command → terminal |
| GET | `/api/threads/{id}/stream` | SSE `StreamEvent` |

Stream query params:

- `mode=checkpoint` (default) — one synthetic event from latest checkpoint
- `mode=resume&kind=approve&payload=ok` — live resume stream
- `mode=invoke&seed=…` — live invoke stream
