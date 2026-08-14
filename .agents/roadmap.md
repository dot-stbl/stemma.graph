# Roadmap — что делаем дальше

> Активная карта работ. **Канон поведения:** main OpenSpec
> [`openspec/specs/`](../openspec/specs/). Planning history:
> [`openspec/changes/archive/2026-08-14-architecture-runtime-core/`](../openspec/changes/archive/2026-08-14-architecture-runtime-core/).

## Текущий статус (2026-08-14)

| | |
|---|---|
| Architecture specs | ✅ archived → `openspec/specs/*` (12 capabilities) |
| MVP runtime | ✅ Pregel + InMemory + HITL + stream + DI + Testing + Generators |
| Samples | ✅ HelloWorld · InterruptResume · AotSmoke · ReviewBot · DocQ · MarketingAgent · MockAdMcp · UiHost |
| Benchmarks | ✅ `benchmarks/Voluta.Benchmarks` (#10 closed) |
| Send / subgraph | ✅ `Send`, `ContinueWithSends`, `Subgraph.AsNode`, `Describe()` |
| File checkpointer | ✅ `Voluta.Checkpoints.File` |
| Agents.AI | ✅ `IGraphNode` adapters for MAF `AIAgent` + MEAI `IChatClient` (`Voluta.MicrosoftAi` removed) |
| Graph DI | ✅ `GraphContext.Services`, `IGraphNode`, `AddNode<T>` |
| UI | ✅ `Voluta.UI` Razor RCL + SSE + `MapVolutaUI` (inspector / HITL / topology) · sample UiHost |
| EF / S3 checkpointers | ✅ `Voluta.Checkpoints.EntityFrameworkCore` · `Voluta.Checkpoints.S3` |
| PublicAPI ship gate | ✅ `PublicAPI.{Shipped,Unshipped}.txt` + PublicApiAnalyzers on ship packages |
| NuGet 0.1 tag | ❌ owner review of Unshipped surface → tag `v0.1.0` + publish |
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
| `Voluta.Checkpoints.EntityFrameworkCore` | provider-agnostic EF Core store | full |
| `Voluta.Checkpoints.S3` | AWS S3 / S3-compatible store | full |
| `Voluta.Agents.AI` | MAF + MEAI as `IGraphNode` | full |
| `Voluta.UI` | ops console | full (ASP.NET) |

## Ближайшие шаги

1. **Owner review of PublicAPI.Unshipped.txt** → tag **`v0.1.0`** + nuget.org publish
2. Architecture tests: Abstractions isolation, core ↛ UI/EF
3. UI polish: multi-thread discovery, richer inspector (SSE shipped — D-025 / #14)
4. Docs site (D-011)

## Не в планах

- Свой LLM SDK · замена MAF · Python-порт · LangSmith clone

## Связанное

- [decisions.md](./decisions.md)
- [conventions.md](./conventions.md)
- Epic #1 · UI #13 · backlog #8
