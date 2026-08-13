# Decisions — принятые решения по StemmaGraph

> Хронологический лог решений. Каждое решение фиксируется здесь **до** того,
> как превращается в код. Канон требований: OpenSpec change
> [`architecture-runtime-core`](../openspec/changes/architecture-runtime-core/).
> ADR — в `.agents/adr/` для нетривиальных обоснований.

## Решения

### D-001 — Имя бренда: **Stemma** (лат. «гирлянда, родословная»)

**Решение:** `Stemma`. Метафора линии состояний через чекпойнты.

### D-002 — Имя первого продукта: **StemmaGraph**

**Решение:** `StemmaGraph` (конкатенация, зеркало LangGraph).

### D-003 — GitHub-репо: `dot-stbl/stemma.graph`

**Решение:** `dot-stbl/stemma.graph`; бренд Stemma независим.

### D-004 — NuGet package ids: `StemmaGraph.*`

**Решение:** `StemmaGraph`, `StemmaGraph.Abstractions`, … (не `Stemma.Graph.*`).

### D-005 — Подход: **не порт LangGraph**, .NET-native переосмысление

**Решение:** концепции (граф, cycles, channels, checkpoint, interrupt, stream);
API — generics, `IAsyncEnumerable`, без TypedDict-рефлексии hot path.

### D-006 — MVP scope: **honest Pregel + InMemory C-checkpoint** *(supersedes scaffold note)*

**Было (scaffold):** MVP без checkpointing.

**Решение (2026-08-13, architecture pass):** MVP 0.1 = full Pregel superstep
(all ready nodes, barrier, versions) + channels (LastValue, Append) +
**C-shape checkpoint** + **InMemory provider in core** + HITL (`NodeResult`) +
multi-mode stream + source-gen skeleton + `StemmaGraph.Testing`.

**Не в 0.1:** Send execution, subgraphs, EF/S3/File packages, UI, MicrosoftAi.

**Почему изменили:** product harness / backend agents без durable pause/resume
и multi-writer merge — не library. Sequential-only B откладывал C-shape и
ломал forward-compat.

### D-007 — MAF / Microsoft.Extensions.AI: через `IChatClient`

**Решение:** не свой LLM SDK. Пакет интеграции — **отдельный**
`StemmaGraph.MicrosoftAi` (post-0.1), не в core.

### D-008 — Технологический стек

| Слой | Технология |
|------|------------|
| Runtime | .NET 10 |
| Тесты | xUnit + Shouldly + NSubstitute + Bogus |
| AI (later) | `Microsoft.Extensions.AI` |
| Streaming | `IAsyncEnumerable<T>` |
| Bench (planned) | BenchmarkDotNet |
| Публикация | NuGet OIDC trusted publishing |

### D-009 — CI/CD

PR: build (warnings-as-errors) + unit (skip Integration). Planned: pack smoke,
openspec validate, expanded path filters. Release: tag `v*.*.*` → pack → nuget.org.

### D-010 — Пакеты (target map)

```
StemmaGraph.Abstractions
StemmaGraph                         # runtime + InMemory checkpointer
StemmaGraph.Testing                 # doubles + conformance (pack carefully)
StemmaGraph.Checkpoints.EntityFrameworkCore   # later
StemmaGraph.Checkpoints.S3                    # later
StemmaGraph.Checkpoints.File                  # later
StemmaGraph.MicrosoftAi                       # later
StemmaGraph.UI.*                              # later (#13)
```

Scaffold ships only Abstractions + StemmaGraph markers until apply.

### D-011 — Docs site: `dot-stbl/stemma-docs`

Отдельный репо; движок/домен — после MVP.

### D-012 — Execution: **full Pregel (C)**

All ready tasks per superstep → barrier → `apply_writes` with channel versions /
`versions_seen`. Not sequential-only B.

### D-013 — State: **channels + reducers**

Named channels; LastValue (single writer/step); Append/binop multi-writer.
Typed `TState` is a view. DX: **source-gen primary** + fluent canon + reflection
fallback at compile-graph (not hot path).

### D-014 — Checkpoint: **full C-shape** + pluggable providers

Snapshot: channel values, versions, versions_seen, pending writes, step, status,
interrupt. InMemory in core; EF/S3/File separate packages. Storage ≠ “files on
disk only”.

### D-015 — HITL: **NodeResult**, not control-flow exceptions

`Continue(writes)` / `Interrupt(payload)`; `ResumeAsync(threadId, Command)`.

### D-016 — Streaming: multi-mode primary API

`values` / `updates` / `events`; `InvokeAsync` drains to terminal.

### D-017 — Hosting: **Both**

Standalone `CompiledGraph<T>` + DI registration of the same instance / runner.

### D-018 — Send / subgraphs: designed, not MVP-shipped

Specs reserve PUSH/Send and subgraph composition; implement post-0.1.

### D-019 — Testing: **`StemmaGraph.Testing`** + conformance

Recording/fault-injecting checkpointer, stream capture, fixtures, InMemory
conformance suite in CI. Quality-engineering: unit scenario matrix, BenchmarkDotNet
(non-gating PR), CI pack/openspec.

### D-020 — UI: separate package, post-0.1

`StemmaGraph.UI` — run inspector, HITL queue, topology (MD3). Core must not
reference UI. Epic: github.com/dot-stbl/stemma.graph/issues/13. Mockups:
local artifacts store (not required in repo for 0.1).

### D-021 — Architecture source of truth: OpenSpec

Change: `openspec/changes/architecture-runtime-core/` (proposal, design, 12 specs,
tasks). Validate: `openspec validate architecture-runtime-core --strict`.

## Open questions (remaining)

1. **Command taxonomy** — approve / reject / update-state / opaque payload shapes.
2. **Checkpoint serde** — JSON versioning, polymorphic channel values.
3. **Token-level LLM streaming** — graph stream mode vs node-local (MicrosoftAi).
4. **Docs site** engine/hosting/domain.
5. **InMemory** re-export from Testing? Prefer core only; Testing wraps.
6. **UI host** — `MapStemmaUI()` static assets vs Razor (decide at UI implement).

## GitHub tracking

Milestone: `v0.1 · MVP runtime`. Epic #1; docs #2; abstractions #3; runtime #4;
source-gen #5; Testing #6; samples #7; deferred #8; gap specs #9; benches #10;
CI #11; unit matrix #12; UI epic #13.

## Связанное

- [roadmap.md](./roadmap.md)
- [conventions.md](./conventions.md)
- [`../openspec/changes/architecture-runtime-core/`](../openspec/changes/architecture-runtime-core/)
- [`../CLAUDE.md`](../CLAUDE.md)
