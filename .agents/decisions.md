# Decisions — принятые решения по Voluta

> Хронологический лог решений. Каждое решение фиксируется здесь **до** того,
> как превращается в код. Канон требований: main OpenSpec
> [`openspec/specs/`](../openspec/specs/). Planning archive:
> [`openspec/changes/archive/2026-08-14-architecture-runtime-core/`](../openspec/changes/archive/2026-08-14-architecture-runtime-core/).
> ADR — в `.agents/adr/` для нетривиальных обоснований.

## Решения

### D-001 — Имя бренда: **Stemma** (лат. «гирлянда, родословная») — ОТМЕНЕНО

**Решение:** `Stemma`. Метафора линии состояний через чекпойнты.
**Статус:** отменено [D-024](#d-024--переименование-в-voluta). Текст сохранён как история —
имя действительно значило «гирлянда, родословная», и переписывать это задним числом нельзя.

### D-002 — Имя первого продукта: **StemmaGraph** — ОТМЕНЕНО

**Решение:** `StemmaGraph` (конкатенация, зеркало LangGraph).
**Статус:** отменено [D-024](#d-024--переименование-в-voluta) — именно мотив «зеркало LangGraph»
и оказался проблемой, а не преимуществом.

### D-003 — GitHub-репо: `dot-stbl/stemma.graph` — ПЕРЕИМЕНОВАНО

**Решение:** `dot-stbl/stemma.graph`; бренд Stemma независим.
**Статус:** репо переименовано в `dot-stbl/voluta` по [D-024](#d-024--переименование-в-voluta);
GitHub держит редирект со старого пути.

### D-004 — NuGet package ids: `Voluta.*`

**Решение:** `Voluta`, `Voluta.Abstractions`, … (не `Voluta.Graph.*`).

### D-005 — Подход: **не порт LangGraph**, .NET-native переосмысление

**Решение:** концепции (граф, cycles, channels, checkpoint, interrupt, stream);
API — generics, `IAsyncEnumerable`, без TypedDict-рефлексии hot path.

### D-006 — MVP scope: **honest Pregel + InMemory C-checkpoint** *(supersedes scaffold note)*

**Было (scaffold):** MVP без checkpointing.

**Решение (2026-08-13, architecture pass):** MVP 0.1 = full Pregel superstep
(all ready nodes, barrier, versions) + channels (LastValue, Append) +
**C-shape checkpoint** + **InMemory provider in core** + HITL (`NodeResult`) +
multi-mode stream + source-gen skeleton + `Voluta.Testing`.

**Не в 0.1:** Send execution, subgraphs, EF/S3/File packages, UI, MicrosoftAi.

**Почему изменили:** product harness / backend agents без durable pause/resume
и multi-writer merge — не library. Sequential-only B откладывал C-shape и
ломал forward-compat.

### D-007 — MAF / Microsoft.Extensions.AI: через `IChatClient`

**Решение:** не свой LLM SDK. Пакет интеграции — **отдельный**
`Voluta.MicrosoftAi` (post-0.1), не в core.

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
# AOT core tier
Voluta.Abstractions
Voluta                         # runtime + InMemory (no ME.DI reference)
Voluta.DependencyInjection     # AddVoluta for IServiceCollection

# Full .NET / ASP.NET tier (not AOT-claimed)
Voluta.Testing                 # doubles + conformance (pack carefully)
Voluta.Checkpoints.EntityFrameworkCore   # later
Voluta.Checkpoints.S3                    # later
Voluta.Checkpoints.File                  # later
Voluta.MicrosoftAi                       # later
Voluta.UI.*                              # later (#13)
```

Scaffold ships only Abstractions + Voluta markers until apply.

### D-011 — Docs site: `dot-stbl/voluta-docs`

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

Standalone `CompiledGraph` (core) + DI via **`Voluta.DependencyInjection`**
(`AddVoluta`). Core has **zero** `Microsoft.Extensions.DependencyInjection`
package reference.

### D-018 — Send / subgraphs: designed, not MVP-shipped

Specs reserve PUSH/Send and subgraph composition; implement post-0.1.

### D-019 — Testing: **`Voluta.Testing`** + conformance

Recording/fault-injecting checkpointer, stream capture, fixtures, InMemory
conformance suite in CI. Quality-engineering: unit scenario matrix, BenchmarkDotNet
(non-gating PR), CI pack/openspec.

### D-020 — UI: separate package, post-0.1

`Voluta.UI` — run inspector, HITL queue, topology (MD3). Core must not
reference UI. Epic: github.com/dot-stbl/voluta/issues/13. Mockups:
local artifacts store (not required in repo for 0.1).

### D-021 — Architecture source of truth: OpenSpec **main specs**

**Update (2026-08-14):** Change `architecture-runtime-core` **archived** as
`openspec/changes/archive/2026-08-14-architecture-runtime-core/`. Canonical
behavior lives in **`openspec/specs/<capability>/spec.md`** (12 capabilities).
Validate: `openspec validate --specs --strict`.

### D-022 — Two runtime tiers: **AOT core** vs **full .NET host packages**

**Decision:** Split the product surface by publish model — no core rewrite.

| Tier | Packages | Target |
|------|----------|--------|
| **AOT core** | `Voluta`, `Voluta.Abstractions`, `Voluta.DependencyInjection` | `IsAotCompatible` + trim analyzer; smoke `samples/03-AotSmoke` (`PublishAot`) |
| **Full runtime** | `Voluta.Checkpoints.File`, `Voluta.UI`, `Voluta.MicrosoftAi`, Testing, Generators | Regular .NET / ASP.NET; may use reflection, JSON, browsers, etc. **Do not** claim AOT |

**AOT path (supported):** fluent `StateGraph` + InMemory (+ optional thin DI).  
**Full path (product hosts):** ASP.NET, file/EF checkpointers, UI console, LLM adapters — depend on core, run on complete CLR.

Checkpoint packages that need JSON must use STJ source-gen (or non-reflection serde) **if** they ever want AOT; until then they stay full-runtime only.

### D-023 — Post-MVP packages shipped on main (before NuGet 0.1)

**Decision (2026-08-14):** After MVP runtime, ship on `main` without waiting for
the NuGet tag:

| Item | Package / API |
|------|----------------|
| Send fan-out | `Send`, `NodeResult.ContinueWithSends`, `PendingSends`, engine PUSH tasks |
| Subgraph helper | `Subgraph.AsNode` |
| Topology export | `CompiledGraph.Describe()` → `GraphDescription` |
| File checkpointer | `Voluta.Checkpoints.File` |
| MicrosoftAi | `Voluta.MicrosoftAi` (`ChatClientNode`) |
| UI | `Voluta.UI` Razor RCL (`AddVolutaUI` / `MapVolutaUI` + SSE) — see D-025 |
| Harness samples | `04-ReviewBot`, `05-DocQ` + `Voluta.Samples.Shared` |
| Benchmarks | `benchmarks/Voluta.Benchmarks` |

Still deferred: EF/S3 checkpointers, PublicAPI ship gate, NuGet `v0.1.0`, arch tests.

### D-024 — Переименование в **Voluta**

**Решение (2026-08-14):** бренд и продукт — `Voluta`. Отменяет D-001 и D-002, переименовывает
репо из D-003. Схема пакетов остаётся плоской по D-004: `Voluta`, `Voluta.Abstractions`, …

**Почему:**

1. **Паттерн «одно брендовое слово» вместо «имя + категория».** `StemmaGraph` зеркалил
   `LangGraph` (мотив D-002) и тем самым навсегда ставил проект в его тень, подрезая собственную
   позицию «не порт, мы вровень».
2. **У `Stemma` два живых омонима в dev-выдаче:** Stemma — data-каталог (деньги Sequoia, куплен
   Teradata) и Adafruit **STEMMA / STEMMA QT** — стандарт разъёма. На NuGet имя `Stemma` было
   свободно: коллизии нашлись только поиском, реестр их не показывал.
3. **`voluta` ← лат. `volvere` «вращать»** — цикл назван в самом имени. Для рантайма из цикличных
   супершагов это точнее, чем метафора родословной. В выдаче — только архитектурный завиток и род
   морских улиток, ни одного продукта. Свободно на NuGet, npm, crates.

**Окно:** 0.1 не затегирован и в NuGet ничего не опубликовано, поэтому цена переименования —
find/replace (793 вхождения в 166 файлах), а не deprecation старых пакетов + redirect-пакет.

**Что изменилось:** неймспейсы и package ids `StemmaGraph.*` → `Voluta.*`; солюшен
`stemma.graph.slnx` → `voluta.slnx`; коммит-префикс `[stemma]` → `[voluta]`; баннер —
`assets/banner-voluta.png` (старый `banner.png` оставлен как история). Условия в
`Directory.Build.props`, завязанные на литеральные имена проектов (AOT-ярус и выбор иконки),
обновлены вместе с именами — иначе они молча перестали бы срабатывать.

### D-025 — UI package: **Razor RCL + SSE + MapVolutaUI options**

**Решение (2026-08-14, issue #14):** `Voluta.UI` is a **Razor Class Library**
(`Microsoft.NET.Sdk.Razor` + `FrameworkReference Microsoft.AspNetCore.App`), not a
thin embedded-HTML-only package. Host integration stays Swagger-style:

```csharp
builder.Services.AddVolutaUI(session);
app.MapVolutaUI(o => o.PathPrefix = "/voluta"); // default /voluta
```

**Live stream:** Server-Sent Events from `IAsyncEnumerable<StreamEvent>`
(`GET {prefix}/api/threads/{threadId}/stream`) — **not** WebSocket / SignalR /
Blazor Interactive Server as the default. Cancel on client disconnect.

**UI chrome:** MD3 dark-ish tokens + Material Symbols; modular static shell
(inspector / HITL / topology). No MudBlazor, no Vite/React monorepo in this package.

**Isolation:** `Voluta` must not reference `Voluta.UI` (full-runtime host package only).

**Sample:** `samples/06-UiHost` — `dotnet run` → `http://localhost:5188/voluta`.

**Out of scope (still):** Interactive Server circuit default, multi-graph host,
auth, full graph canvas editor.

## Open questions (remaining)

1. **Command taxonomy** — approve / reject / update-state / opaque payload shapes.
2. **Checkpoint serde** — JSON versioning, polymorphic channel values (File package is best-effort JSON).
3. **Token-level LLM streaming** — graph stream mode vs node-local (MicrosoftAi).
4. **Docs site** engine/hosting/domain.
5. **InMemory** re-export from Testing? Prefer core only; Testing wraps.
6. **UI next** — multi-thread discovery beyond process-tracked ids; richer inspector.
7. **AOT CI** — optional `dotnet publish` smoke on schedule vs every PR (slow).
8. **0.1 NuGet** — PublicAPI Unshipped review before first tag.

## GitHub tracking

Milestone: `v0.1 · MVP runtime`. Epic #1; benches #10 closed; UI epic #13 (first cut);
backlog #8 (partial — Send/File/AI/UI done; EF/S3 open).

## Связанное

- [roadmap.md](./roadmap.md)
- [conventions.md](./conventions.md)
- [`../openspec/specs/`](../openspec/specs/)
- [`../openspec/changes/archive/2026-08-14-architecture-runtime-core/`](../openspec/changes/archive/2026-08-14-architecture-runtime-core/)
- [`../CLAUDE.md`](../CLAUDE.md)
