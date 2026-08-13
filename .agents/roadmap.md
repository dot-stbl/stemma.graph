# Roadmap — что делаем дальше

> Активная карта работ по StemmaGraph. Обновляется по мере выполнения.
> Источник истины для порядка задач.

## Текущий статус (2026-08-13)

**Scaffolding готов.** Initial commit `0ecf278` на `main`. Конвенции
зафиксированы. Документация в `.agents/decisions.md`. Research по LangGraph
internals запущен (background agent).

**В работе:**
- Research по LangGraph internals → `.agents/research/langgraph-internals.md`
  (когда вернётся).

## Ближайшие шаги

### 1. Research → `.agents/research/`

- Прочитать LangGraph source (Pregel, Channels, StateGraph, Checkpointing,
  Interrupts, Subgraphs, Streaming).
- Извлечь что стоит заимствовать, что переделать, где болит.
- Готово → переход к шагу 2.

### 2. Архитектурный проход

- Решить, что из LangGraph переносится (см. [decisions.md D-005](./decisions.md#d-005--подход-не-порт-langgraph-net-native-переосмысление)).
- Зафиксировать ответы на [open questions](./decisions.md#open-questions-на-момент-2026-08-13):
  - persistence: EF Core или абстрактный checkpointer?
  - channels/reducers: какая форма для .NET?
  - subgraphs: в каком виде?
  - MAF integration: отдельный пакет или часть основного?
- Возможно — отдельный ADR для каждого неочевидного решения.

### 3. MVP

Цель первого релиза: `StemmaGraph` 0.1.0 с минимальным runtime.

**Что входит:**

- [ ] `StateGraph<TState>` — fluent builder API
- [ ] `CompiledGraph` с `InvokeAsync` / `StreamAsync`
- [ ] Conditional edges + циклы
- [ ] In-memory execution (без checkpointing)
- [ ] Один рабочий sample (`samples/01-HelloWorld` — реальный, не canary)
- [ ] Smoke-тесты → реальные unit-тесты на ядро
- [ ] PublicAPI tracking (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt`)
- [ ] Публикация 0.1.0 в NuGet

**Что НЕ входит:**

- Checkpointing (см. D-006)
- Subgraphs
- MAF / Microsoft.Extensions.AI интеграция
- Визуализатор

### 4. После MVP

**v0.2 — persistence (когда решение принято):**
- Либо EF Core-подход (миграции + DbContext), либо `ICheckpointer<T>` +
  бэкенды (если решим идти путём LangGraph).
- SQLite в первую очередь, Postgres — если нужно.

**v0.3 — MAF / Microsoft.Extensions.AI:**
- Решить форму пакета (см. open question #4).
- Пример с реальным провайдером (OpenAI / Ollama / Azure).

**v0.4 — subgraphs:**
- Форма (вложенные `CompiledGraph` или `AddSubgraph(...)`).
- Propagated state через `IStateMerger<T>`.

**v0.5 — interrupts / HITL:**
- `Interrupt()` в узлах.
- `Command` для resume.

**v0.6 — observability:**
- OpenTelemetry activity source per node.
- Channel/reducer events.
- Integration с существующими `.NET` OTel-инструментами.

**v1.0:**
- Стабильный API (semver).
- Полная документация на docs-сайте.
- ≥5 samples.
- Performance baseline.

## Не в планах

- Свой LLM SDK (используем `Microsoft.Extensions.AI`).
- Замена Microsoft Agent Framework (MAF и StemmaGraph комплементарны).
- Python-порт (целевая аудитория — .NET-комьюнити).

## Связанное

- [decisions.md](./decisions.md) — принятые решения + open questions.
- [conventions.md](./conventions.md) — где живут конвенции.
- [`../CLAUDE.md`](../CLAUDE.md) — handbook.