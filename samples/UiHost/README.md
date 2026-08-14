# UiHost — Voluta ops console sample

Minimal `WebApplication` that hosts **Voluta.UI** (Razor Class Library shell +
JSON API + SSE live stream) with a multi-node demo graph and seeded threads.

## Run

```bash
dotnet run --project samples/UiHost
```

Open:

```
http://localhost:5188/voluta
```

Port is fixed in `Properties/launchSettings.json`. Do not leave `dotnet run`
running in agent sessions (agent-runtime-safety).

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
