# StudioHost

ASP.NET sample that exposes the **versioned Studio HTTP/SSE API** (`MapStudioApi`)
for SPA / ops tooling. Same multi-node demo graph and seeded threads as
[`UiHost`](../UiHost/), without the Razor UI shell.

## Run

```bash
dotnet run --project samples/StudioHost
```

Base URL: `http://localhost:5189`

## Endpoints (`/api/v1`)

| Method | Path | Maps to |
|--------|------|---------|
| GET | `/api/v1/topology` | `CompiledGraph.Describe()` |
| GET | `/api/v1/threads` | discovery + summaries |
| GET | `/api/v1/threads/{id}` | `GetStateAsync` |
| GET | `/api/v1/threads/{id}/history` | `GetHistoryAsync` |
| POST | `/api/v1/threads/{id}/resume` | `ResumeInvokeAsync` |
| POST | `/api/v1/threads/{id}/continue` | `ContinueInvokeAsync` |
| POST | `/api/v1/threads/{id}/update` | `UpdateStateAsync` |
| POST | `/api/v1/threads/{id}/fork` | `ForkAsync` |
| GET | `/api/v1/threads/{id}/stream` | SSE events |
| GET | `/api/v1/hitl` | interrupted threads |

Contract details: [`docs/0.x/studio-api.mdx`](../../docs/0.x/studio-api.mdx).

## Optional API key

Set `StudioApi:ApiKey` in config or env `StudioApi__ApiKey`. Clients send
`X-Api-Key: …` or `Authorization: Bearer …`. Empty/null = auth off (default).

## Seeded threads

- `payment-hitl` / `deploy-hitl` — Interrupted at `risk_gate`
- `research-done` — Done
- `audit-blocked` — Done after reject resume
