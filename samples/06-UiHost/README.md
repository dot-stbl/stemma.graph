# 06-UiHost — Voluta ops console sample

Minimal `WebApplication` that hosts **Voluta.UI** (Razor Class Library shell + JSON API + **SSE** live stream).

## Run

```bash
dotnet run --project samples/06-UiHost
```

Open in a browser:

```
http://localhost:5188/voluta
```

On startup the sample:

1. Compiles a small HITL graph (`START → gate → END`) with an in-memory checkpointer.
2. Invokes thread `ui-host-hitl-1` once so `gate` interrupts.
3. Calls `TrackThread` so the HITL queue is non-empty.

## Screens

| Tab | What |
|-----|------|
| Run inspector | Load checkpoint by thread id; optional SSE (`GET …/stream`) |
| HITL queue | Interrupted threads → Approve / Reject / SSE resume |
| Topology | `CompiledGraph.Describe()` nodes, edges, channels |

## Host wiring

```csharp
var session = new VolutaUiSession(graph, checkpointer);
builder.Services.AddVolutaUI(session);
app.MapVolutaUI(options => options.PathPrefix = "/voluta");
```

## API (under `/voluta`)

| Method | Path | Role |
|--------|------|------|
| GET | `/` | UI shell |
| GET | `/api/topology` | Graph description |
| GET | `/api/hitl` | Interrupted threads |
| GET | `/api/threads/{id}` | Checkpoint |
| POST | `/api/threads/{id}/resume` | Command → terminal |
| GET | `/api/threads/{id}/stream` | SSE `StreamEvent` |

Stream query params:

- `mode=checkpoint` (default) — one synthetic event from latest checkpoint
- `mode=resume&kind=approve&payload=ok` — live resume stream
- `mode=invoke&seed=…` — live invoke stream

## Notes

- Do not leave `dotnet run` running in agent sessions (agent-runtime-safety).
- Port is fixed in `Properties/launchSettings.json` (`http://localhost:5188`).
