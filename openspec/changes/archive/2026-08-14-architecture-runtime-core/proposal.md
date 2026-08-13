## Why

.NET has no first-class equivalent of LangGraph for long-running, stateful agent graphs. Product harnesses need cycles, shared state with multi-writer merge, durable checkpoint/resume, HITL, and streaming — without Python and without CLI/Studio fluff. This change locks the architecture for Voluta so implementation can follow a single, agreed design.

## What Changes

- Adopt **Pregel-style execution (full C)**: all ready nodes per superstep, barrier, channel merge with versions.
- Define **channels + reducers** as the state model; source-gen + fluent + reflection as layered DX (flexibility preserved).
- Define **full C-shape checkpoints** and pluggable providers (InMemory in core; EF Core, S3, File, optional graph DB as packages).
- Define **HITL** via `NodeResult` (not exceptions) + `ResumeAsync` / `Command`.
- Define **multi-mode streaming** (`values` / `updates` / `events`) as primary API; `InvokeAsync` as convenience.
- Define **hosting Both**: standalone `CompiledGraph<T>` and DI registration.
- Design **Send fan-out and subgraphs** (not shipped in MVP 0.1; contracts in specs).
- Introduce **`Voluta.Testing`** package: recording/fault-injecting checkpointers, stream capture, graph fixtures, checkpoint conformance suite.
- Define **graph topology** (nodes, edges, conditional edges, compile-time validation).
- Define **error and cancellation** semantics for failed nodes, failed supersteps, and cooperative cancel.
- Define **quality engineering**: unit scenario matrix, benchmarks project, CI gates (build/test/pack/openspec).
- Document MVP = honest Pregel core + InMemory; providers and UI packages later.
- **No production runtime code in this change** — planning artifacts only; code lands in follow-up apply changes.

## Capabilities

### New Capabilities

- `graph-runtime`: Pregel superstep loop, ready-set, barrier, apply_writes, recursion limits, START/END.
- `state-channels`: named channels, LastValue / Append (binop) reducers, state schema compile, partial updates → channel writes.
- `checkpoint`: C-shape snapshot, `ICheckpointer`, InMemory provider, provider package map (EF/S3/File later).
- `hitl-interrupt`: NodeResult continue/interrupt, Command resume, interrupt payload in checkpoint.
- `streaming`: StreamMode values/updates/events, IAsyncEnumerable surface, InvokeAsync composition.
- `public-api-hosting`: StateGraph builder, CompiledGraph, fluent + DI Both, compile-once lifetime.
- `source-gen-state`: `[GraphState]` → TState / Update / Schema / ToWrites; escape hatches without gen.
- `send-subgraphs`: Send fan-out and subgraph composition (design + requirements; out of MVP ship).
- `testing-providers`: Voluta.Testing helpers + checkpoint conformance suite.
- `graph-topology`: AddNode/AddEdge/conditional edges, START/END wiring, compile validation.
- `error-cancellation`: node/superstep failure, run status failed, cancellation behavior.
- `quality-engineering`: unit scenario map, BenchmarkDotNet project, CI pack/openspec gates.

### Modified Capabilities

- (none — greenfield; no existing `openspec/specs/` behavior yet)

## Impact

- **Packages (target layout):** `Voluta.Abstractions`, `Voluta` (runtime + InMemory), `Voluta.Testing`, later `Voluta.Checkpoints.*`, optional UI/AI packages.
- **Repo docs:** decisions/roadmap align with this change; public README stays high-level until first ship.
- **Dependencies:** none new until apply; design assumes .NET 10, `Microsoft.Extensions.AI` at integration boundary (not core).
- **Non-goals:** LangSmith/CLI/Studio clones; own LLM SDK; replacing MAF.
