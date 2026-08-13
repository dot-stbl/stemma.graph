# Roadmap — что делаем дальше

> Активная карта работ. Канон требований: OpenSpec
> `openspec/changes/architecture-runtime-core/`. Issues: milestone
> `v0.1 · MVP runtime` on `dot-stbl/stemma.graph`.

## Текущий статус (2026-08-13)

| | |
|---|---|
| Scaffold | ✅ solution, packages markers, CI/publish workflows |
| Architecture | ✅ OpenSpec `architecture-runtime-core` (strict valid) |
| Decisions | ✅ D-001…D-021 in [decisions.md](./decisions.md) |
| Runtime code | ❌ not started |
| UI mockups | ✅ local MD3 HTML (artifacts); package later (#13) |

## Ближайшие шаги (v0.1)

Порядок apply (можно parallel worktrees где независимо):

| Order | Issue | Work |
|------:|-------|------|
| 1 | #2 | docs align (this file + decisions — in progress) |
| 2 | #3 | `StemmaGraph.Abstractions` contracts |
| 3 | #4 | Pregel runtime + InMemory + HITL + stream |
| 4 | #5 | source-gen `[GraphState]` |
| 5 | #6 | `StemmaGraph.Testing` + conformance |
| 6 | #12 | unit scenario matrix |
| 7 | #10 / #11 | benches + CI hardening (can overlap late) |
| 8 | #7 | samples + ship gate |

**#9** (gap specs) — largely done in OpenSpec; close after owner skim.

### MVP 0.1 includes

- [ ] Pregel: all ready, barrier, apply_writes, versions
- [ ] Channels: LastValue, Append
- [ ] Topology: nodes, edges, conditional, compile validation
- [ ] C-shape checkpoint + InMemory provider
- [ ] NodeResult interrupt/resume
- [ ] Stream values/updates/events + InvokeAsync
- [ ] Hosting Both (fluent + DI)
- [ ] Source-gen skeleton
- [ ] StemmaGraph.Testing + InMemory conformance in CI
- [ ] Unit scenario matrix + samples (ReAct + HITL)
- [ ] PublicAPI Unshipped; packable core only

### MVP 0.1 excludes

- Send execution, subgraphs (designed only)
- EF / S3 / File checkpointer packages
- `StemmaGraph.MicrosoftAi`
- `StemmaGraph.UI` (#13)
- OTel-deep, time-travel UX

## После 0.1

| Track | Issues / notes |
|-------|----------------|
| Persistence providers | #8 → EF first, then S3/File |
| Send + subgraphs | #8 + send-subgraphs spec |
| MicrosoftAi | #8 |
| UI console | #13 (inspector, HITL, topology, MD3) |
| Observability | OTel per node |
| Docs site | D-011 |
| v1.0 | semver freeze, ≥5 samples, perf baselines |

## Не в планах

- Свой LLM SDK
- Замена MAF
- Python-порт
- LangSmith/CLI/Studio clone

## Связанное

- [decisions.md](./decisions.md)
- [OpenSpec change](../openspec/changes/architecture-runtime-core/)
- Epic: https://github.com/dot-stbl/stemma.graph/issues/1
