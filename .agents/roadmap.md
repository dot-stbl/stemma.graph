# Roadmap — что делаем дальше

> Активная карта работ. Канон: OpenSpec
> `openspec/changes/architecture-runtime-core/`.

## Текущий статус (2026-08-13)

| | |
|---|---|
| Architecture | ✅ OpenSpec `architecture-runtime-core` |
| MVP runtime | ✅ Pregel + InMemory + HITL + stream + source-gen + Testing |
| Benchmarks | ✅ `benchmarks/StemmaGraph.Benchmarks` (#10 closed) |
| Send / subgraph | ✅ `Send`, `ContinueWithSends`, `Subgraph.AsNode`, `Describe()` |
| File checkpointer | ✅ `StemmaGraph.Checkpoints.File` |
| MicrosoftAi | ✅ thin helpers |
| UI | ✅ `StemmaGraph.UI` + `MapStemmaUI` (inspector / HITL / topology) |
| EF / S3 | ❌ later |
| NuGet 0.1 tag | ❌ PublicAPI review + publish |

## Ближайшие шаги

1. PublicAPI review + `v0.1.0` tag / NuGet (when you care)
2. EF checkpointer (if needed)
3. UI polish (live stream SSE, multi-thread browser)
4. Docs site (D-011)

## Не в планах

- Свой LLM SDK · замена MAF · Python-порт · LangSmith clone

## Связанное

- [decisions.md](./decisions.md)
- Epic #1 · UI #13 · backlog #8
