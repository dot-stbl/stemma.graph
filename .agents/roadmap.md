# Roadmap — что делаем дальше

> Активная карта работ. **Канон поведения:** main OpenSpec
> [`openspec/specs/`](../openspec/specs/). Planning history:
> [`openspec/changes/archive/2026-08-14-architecture-runtime-core/`](../openspec/changes/archive/2026-08-14-architecture-runtime-core/).

## Текущий статус (2026-08-14)

| | |
|---|---|
| Architecture specs | ✅ archived → `openspec/specs/*` (12 capabilities) |
| MVP runtime | ✅ Pregel + InMemory + HITL + stream + DI + Testing + Generators |
| Samples | ✅ 01 ReAct · 02 HITL · 03 AOT · 04 ReviewBot · 05 DocQ |
| Benchmarks | ✅ `benchmarks/Voluta.Benchmarks` (#10 closed) |
| Send / subgraph | ✅ `Send`, `ContinueWithSends`, `Subgraph.AsNode`, `Describe()` |
| File checkpointer | ✅ `Voluta.Checkpoints.File` |
| MicrosoftAi | ✅ thin `IChatClient` helpers |
| UI | ✅ `Voluta.UI` + `MapVolutaUI` (inspector / HITL / topology) |
| EF / S3 checkpointers | ❌ later |
| NuGet 0.1 tag | ❌ PublicAPI review + publish |
| Arch tests (package isolation) | ❌ later |

## Shipped package map

| Package | Role | Tier |
|---------|------|------|
| `Voluta.Abstractions` | contracts | AOT |
| `Voluta` | runtime + InMemory + Subgraph | AOT |
| `Voluta.DependencyInjection` | `AddVoluta` | AOT |
| `Voluta.Testing` | doubles + conformance | full |
| `Voluta.Generators` | `[GraphState]` | full (analyzer) |
| `Voluta.Checkpoints.File` | JSON file store | full |
| `Voluta.MicrosoftAi` | MEAI helpers | full |
| `Voluta.UI` | ops console | full (ASP.NET) |

## Ближайшие шаги

1. **PublicAPI Unshipped + `v0.1.0` NuGet tag** (owner call)
2. CI: path filters for `openspec/**` / `benchmarks/**`; optional pack smoke
3. Architecture tests: Abstractions isolation, core ↛ UI/EF
4. EF checkpointer (if product needs it)
5. UI polish: live stream SSE, multi-thread discovery
6. Docs site (D-011)

## Не в планах

- Свой LLM SDK · замена MAF · Python-порт · LangSmith clone

## Связанное

- [decisions.md](./decisions.md)
- [conventions.md](./conventions.md)
- Epic #1 · UI #13 · backlog #8
