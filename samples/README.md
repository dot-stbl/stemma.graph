# Samples

Runnable demos for the Voluta runtime. All projects are in `voluta.slnx` under
`/samples/` and are **not** packable NuGet packages.

## Catalog

| Project | Shows |
|---------|-------|
| [`HelloWorld`](HelloWorld/) | Cyclic agent ⇄ tools (simulated ReAct), `StreamMode.Updates` |
| [`InterruptResume`](InterruptResume/) | HITL interrupt + `ResumeAsync` with `Command` |
| [`AotSmoke`](AotSmoke/) | Minimal linear graph + Native AOT publish smoke |
| [`ReviewBot`](ReviewBot/) | CLI harness: plan → sandbox tools → review (+ optional HITL) |
| [`DocQ`](DocQ/) | Docs Q&A: search sandbox → answer with citations |
| [`MarketingAgent`](MarketingAgent/) | Hybrid desk: brief → creative → create RK/SSP/banner → review |
| [`MockAdMcp`](MockAdMcp/) | Hybrid-shaped MCP tools (Campaign/SSP/DirectDeal/AdLibrary) |
| [`UiHost`](UiHost/) | ASP.NET host for `Voluta.UI` + seeded multi-thread demo |
| [`StudioHost`](StudioHost/) | Versioned Studio HTTP/SSE API (`/api/v1`) for SPA clients |
| [`WorkerHost`](WorkerHost/) | Durable `BackgroundService` runner: wake → run → park/complete/fail |

Shared helpers live in [`Voluta.Samples.Shared`](Voluta.Samples.Shared/):
`CliUi` (Claude Code-style terminal chrome), sandbox FS, chat client, CLI flags.
CLI harnesses use `CliUi`. AotSmoke stays minimal (publish smoke only).

Suggested order for first read: HelloWorld → InterruptResume → WorkerHost →
ReviewBot → DocQ → MarketingAgent (+ MockAdMcp) → UiHost → StudioHost.

## Quick start

```bash
dotnet run --project samples/HelloWorld
dotnet run --project samples/InterruptResume
dotnet run --project samples/AotSmoke
dotnet run --project samples/ReviewBot -- --offline --root .
dotnet run --project samples/DocQ -- --offline --root . --question "What is Voluta?"

# marketing harness + mock MCP (two terminals)
dotnet run --project samples/MockAdMcp
dotnet run --project samples/MarketingAgent -- --offline

dotnet run --project samples/UiHost   # then open http://localhost:5188/voluta
dotnet run --project samples/StudioHost   # API http://localhost:5189/api/v1
dotnet run --project samples/WorkerHost
```

Live chat for ReviewBot / DocQ (optional):

```bash
export VOLUTA_CHAT_ENDPOINT=https://api.openai.com/v1
export VOLUTA_CHAT_API_KEY=...
export VOLUTA_CHAT_MODEL=gpt-4o-mini
```

## Layout

- One folder per sample: `Program.cs` + `*.csproj` + `README.md`
- Folder name = project name (no numeric prefixes)
- `Voluta.Samples.Shared` is a library, not a runnable sample
