## Context

See `proposal.md` for why. Repo is scaffolding only (`StemmaGraph` + `StemmaGraph.Abstractions` markers). Conceptual source: LangGraph (MIT) Pregel loop, channels, C-shape checkpoints — researched from clone under temp `langgraph-src` / `langgraph-explained.md`. Constraints: .NET 10, backend-first, no CLI/Studio fluff, house rules (no control-flow exceptions, primary ctors, etc.). **This change is architecture/planning only; no runtime code.**

## Goals / Non-Goals

**Goals:**

- Single coherent architecture for Pregel C execution, channels, C-checkpoints, HITL, streaming, dual hosting, source-gen DX, Send/subgraph forward-compat, Testing package.
- Specs + design sufficient to implement MVP 0.1 (honest Pregel + InMemory) without reopening product questions.
- Package boundaries that keep core free of EF/S3 deps.

**Non-Goals:**

- Implementing runtime, packages, or samples in this change.
- Shipping EF/S3/File/UI/AI packages in MVP.
- Bit-for-bit Python LangGraph interop or LangSmith.
- Replacing Microsoft Agent Framework.

## Decisions

### D1 — Full Pregel superstep (not sequential-only)

**Choice:** All ready tasks per superstep + barrier + `apply_writes`, with channel versions / versions_seen.

**Why:** Product needs true multi-writer and future Send; sequential B would force redesign of checkpoint and merge.

**Alternatives:** Sequential B (faster MVP, wrong end-state); hybrid “B API / C later” (versions bolted on painfully).

### D2 — Channels as state substrate

**Choice:** Named `IChannel` with LastValue + Append (binop) minimum; Topic/Barrier later.

**Why:** Multi-writer safety is the point of C; typed `TState` is a view.

**Alternatives:** Single mutable record replace (clobber); reducers only without channel abstraction (harder checkpoint).

### D3 — DX layering: gen + fluent + reflection

**Choice:** Source-gen primary for app graphs; fluent canon for runtime/dynamic; reflection optional fallback at compile-graph time, not hot path.

**Why:** Best DX without trapping advanced users; gen is façade over `ChannelWrite[]`.

**Alternatives:** Fluent-only (stringly); attributes+reflection only (slow/fragile).

### D4 — Node results, not interrupt exceptions

**Choice:** `NodeResult.Continue(writes)` / `NodeResult.Interrupt(payload)`; resume via `ResumeAsync(threadId, Command)`.

**Why:** Aligns with project exception rules; serializable control flow.

**Alternatives:** LangGraph-style throw; special `__interrupt__` channel only.

### D5 — Checkpoint C-shape + provider packages

**Choice:** Full C snapshot in Abstractions; InMemory in core; EF/S3/File/graph as separate packages; Testing package for doubles + conformance.

**Why:** Backend storage varies; mid-barrier resume needs pending writes; conformance mirrors LangGraph `checkpoint-conformance`.

**Alternatives:** State-only snapshot (insufficient for C); all providers in core (deps hell).

### D6 — Streaming multi-mode as primary API

**Choice:** `IAsyncEnumerable` + modes values/updates/events; `InvokeAsync` drains to terminal.

**Why:** Backend → SSE/UI without second observation model.

### D7 — Hosting Both

**Choice:** `CompiledGraph<T>` standalone; DI registers same instance + optional `IGraphRunner`.

**Why:** Library samples and ASP.NET product share one runtime.

### D8 — Send / subgraphs designed for 0.1; **shipped on main post-MVP**

**Choice (design-time):** Specs + reserved task/PUSH/pending shapes; implement after 0.1.

**Update (implemented):** `Send` + `ContinueWithSends`, engine PUSH ready tasks, `PendingSends` on checkpoint; `Subgraph.AsNode`; `CompiledGraph.Describe` for UI.

### D9 — StemmaGraph.Testing as real package

**Choice:** Separate project/package: recording + fault-injecting checkpointer, stream capture, fixtures, conformance suite entrypoints.

**Why:** Provider authors (including us) need shared tests; keeps core free of test-only types pollution while remaining publishable later for external checkpointer authors.

### D10 — MVP = honest Pregel core

**Choice:** Real parallel ready-set + InMemory C-checkpoint + interrupt/stream + source-gen skeleton; no EF/UI/Send ship.

**Coverage:** ~runtime core; not full ecosystem (see proposal impact).

### D11 — Topology + faults as first-class specs

**Choice:** Explicit `graph-topology` and `error-cancellation` capabilities (not only implied by runtime).

**Why:** MVP implementers need compile validation and fail/cancel semantics; gaps found in coverage review.

### D12 — Quality engineering in-repo

**Choice:** Unit scenario matrix + InMemory conformance in CI; BenchmarkDotNet project non-gating on PR; pack smoke + openspec validate; Testing packability guarded.

**Why:** Library without perf baselines and pack/CI discipline regresses silently; PR-gating benches too noisy for 0.1.

## Package map (target)

```
StemmaGraph.Abstractions     ICheckpointer, CheckpointSnapshot, ChannelWrite, NodeResult, Send, …
StemmaGraph                 runtime, builder, InMemory checkpointer, Subgraph helper
StemmaGraph.DependencyInjection  AddStemmaGraph
StemmaGraph.Testing         recording/fault/stream capture/fixtures/conformance
StemmaGraph.Checkpoints.File                  (shipped)
StemmaGraph.MicrosoftAi                       (shipped, thin IChatClient helpers)
StemmaGraph.UI                                (shipped, MapStemmaUI first cut)
StemmaGraph.Checkpoints.EntityFrameworkCore   (later)
StemmaGraph.Checkpoints.S3                    (later)
```

## Runtime loop (reference)

```
load checkpoint | seed input
while true:
  tasks = prepare_all_ready(channels, versions, send_queue)
  if tasks empty: status=done; break
  if step > limit: fail out_of_steps; break
  results = run_parallel(tasks)   // barrier
  if any interrupt result: apply partial policy; put interrupted; break
  apply_writes(results)           // per-channel update(values[])
  bump versions; update versions_seen
  put checkpoint (C-shape)
  emit stream items
```

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| C complexity delays first NuGet | Strict MVP cut: no Send/EF/UI; conformance on InMemory only |
| Source-gen + fluent dual path confusion | Docs: “typed path default; fluent for dynamic” |
| Mid-barrier side effects (non-idempotent tools) | Pending writes + docs: tools should be idempotent or external outbox |
| Checkpoint schema evolution | FormatVersion on snapshot; conformance tests |
| Over-scoping Testing package | Start with Memory conformance + recording; publish NuGet when external providers exist |
| Accidental nuget publish of Testing | `IsPackable=false` or release allowlist |
| CI path filters skip tests-only PRs | Include `tests/**`, `openspec/**`, `benchmarks/**` in ci paths |
| Node throw vs NodeResult ambiguity | Spec: uncaught throw → failed run; interrupt only via result |

## Migration Plan

N/A for greenfield. Future: archive this change into `openspec/specs/*`; implementation changes reference those specs. No production users yet — no wire compat burden until 0.1 publish (then semver + PublicAPI analyzers).

## Open Questions

- Exact `Command` taxonomy (approve / reject / update-state / resume-with-payload) — fine-tune at implement.
- Whether InMemory lives only in core or also re-exported from Testing — prefer core only; Testing wraps it.
- Token-level LLM streaming as stream mode vs node-local events — defer to MicrosoftAi package design.
- Graph DB checkpointer: keep as optional future package name only until a real need appears.
