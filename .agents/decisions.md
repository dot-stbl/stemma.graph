# Decisions — принятые решения по StemmaGraph

> Хронологический лог решений, принятых в сессии 2026-08-13. Каждое решение
> фиксируется здесь **до** того, как превращается в код. Решения, требующие
> обоснования, дополняются ADR (см. `.agents/adr/`).

## Решения

### D-001 — Имя бренда: **Stemma** (лат. «гирлянда, родословная»)

**Контекст:** нужен короткий бренд для семейства OSS-продуктов по .NET-агентам.

**Решение:** `Stemma`.

**Почему:** метафора «линия состояний через чекпойнты, как родословная через
поколения» — точно описывает stateful-граф с time-travel (если он будет).
Стиль совпадает с другими проектами владельца в `stbl/`: `tessera`, `plexor`,
`anlytra` — короткая латынь / изобретённое слово, мягкое окончание.

**Альтернативы, рассмотренные:** AgentFlow, Mesh, Plexus, Helix, Forge,
Nexor, Nexix, Copula, Catena. Отвергнуты как generic / занятые / не попадающие
в стиль `stbl/`.

### D-002 — Имя первого продукта: **StemmaGraph**

**Контекст:** первый продукт — граф-рантайм для .NET-агентов (аналог
LangGraph).

**Решение:** `StemmaGraph` (конкатенация бренд + продукт).

**Почему:** зеркало LangChain Inc (`LangChain` / `LangGraph` / `LangSmith`).
Конкатенация выбрана сознательно, отходя от типичной .NET-конвенции
`Stemma.Graph.*` — ради близости к источнику вдохновения и удобства
произношения.

### D-003 — GitHub-репо: `dot-stbl/stemma.graph`

**Контекст:** где публиковать код.

**Решение:** репозиторий `dot-stbl/stemma.graph` под оргой `dot-stbl` —
личный GitHub владельца. Бренд Stemma при этом остаётся независимым
(домен, NuGet-префикс, branding — своё).

**Почему:** `dot-stbl` уже содержит публичные OSS-проекты владельца
(`regent`, `tessera`, `plexor`) — логичная посадка для ещё одного OSS.

### D-004 — NuGet package ids: `StemmaGraph.*`

**Контекст:** какие имена давать NuGet-пакетам.

**Решение:** `StemmaGraph`, `StemmaGraph.Abstractions`, `StemmaGraph.*`
(конкатенация, не `Stemma.Graph.*` через точку).

**Почему:** см. D-002 — консистентно с брендом. Отходим от типичной .NET-конвенции
(`MassTransit`, `Aspire`, `Polly` — через точку) сознательно. Namespace
зеркалит package id: `StemmaGraph.*`.

### D-005 — Подход: **не порт LangGraph**, .NET-native переосмысление

**Контекст:** что забираем из LangGraph, что переделываем.

**Решение:**
- **Забираем концепции:** граф как first-class, циклы, conditional edges,
  checkpointing (опционально), channels/reducers (опционально), interrupts
  (опционально), state lineage.
- **Не переносим 1:1:** Pydantic → `record` с `init` + `required`;
  TypedDict → `StateGraph<TState>` где `TState : class`; `asyncio.Queue` →
  `System.Threading.Channels`; `async generator` → `IAsyncEnumerable<T>`.
- **Главное отличие:** .NET-native generic state с типобезопасностью на
  этапе компиляции, а не runtime-проверкой через TypedDict.

**Почему:** иначе получится обёртка над Python-семантикой, а не идиоматичный
.NET-фреймворк.

### D-006 — MVP scope: минимальный, **без checkpointing / persistence**

**Контекст:** что входит в первый релиз.

**Решение:** MVP = `StateGraph<TState>` + in-memory execution + один sample.
Без checkpointing, без персистенции, без подграфов, без MAF-интеграции.

**Почему:** checkpointing — самая сложная и спорная часть LangGraph
(расходует усилия на дизайн, ошибки дорогие). Сначала доказать, что
runtime + fluent builder работают, потом добавлять слои.

**Open question:** какая персистенция в принципе нужна? Если да —
EF Core (один DbContext + миграции, всё в основном пакете) или выделенный
абстрактный checkpointer с бэкендами (как у LangGraph)? Решается после
research по LangGraph internals.

### D-007 — MAF / Microsoft.Extensions.AI: интеграция через `IChatClient`

**Контекст:** какой AI-абстракцией пользоваться.

**Решение:** `Microsoft.Extensions.AI.IChatClient` — единая точка для любого
провайдера. StemmaGraph не делает свой LLM-слой; узлам доступен `IChatClient`
через DI.

**Почему:** `IChatClient` — Microsoft-blessed абстракция, держит весь
спектр провайдеров (OpenAI, Azure, Ollama, Anthropic и т.д.). Не плодим
своих интерфейсов.

**Open question:** форма пакета для интеграции — отдельный
`StemmaGraph.MicrosoftAi` (как `MassTransit.AzureServiceBus`) или
часть основного `StemmaGraph`? Решается после MVP.

### D-008 — Технологический стек

| Слой | Технология |
|------|------------|
| Runtime | .NET 10 |
| Тесты | xUnit + Shouldly + NSubstitute + Bogus |
| AI integration | `Microsoft.Extensions.AI` (см. D-007) |
| Streaming | `IAsyncEnumerable<T>` + `System.Threading.Channels` |
| Публикация | NuGet через GitHub Actions OIDC trusted publishing (без долгоживущих API-ключей) |

### D-009 — CI/CD

- **PR gate:** `dotnet build stemma.graph.slnx -c Debug` (warnings-as-errors)
  + `dotnet test --filter "FullyQualifiedName!~Integration"`.
- **Release:** push тега `v*.*.*` → pack → push в nuget.org через OIDC
  trusted publishing.
- **Runner:** `ubuntu-latest` (не нужен self-hosted — чистый .NET).

### D-010 — Пакеты на старте: **только два**

**Контекст:** какие NuGet-пакеты реально создавать на этапе скаффолда.

**Решение:** `StemmaGraph` (runtime) + `StemmaGraph.Abstractions` (interfaces).
Всё остальное (checkpointers, MAF-bridge) добавляется **после** архитектурного
обсуждения и research по LangGraph internals.

**Почему:** была попытка сделать `.Checkpoints.Memory/.Sqlite/.Postgres` и
`.MicrosoftAi` сразу — отвергнута владельцем как преждевременная
(checkpoints — фича LangGraph, может не понадобиться; пакетная форма —
архитектурное решение, не обсуждено).

### D-011 — Документация и docs-сайт

**Контекст:** у владельца есть домен и хостинг; хочет полноценный docs-сайт
для StemmaGraph, а не просто GitHub Pages.

**Решение:** отдельный репозиторий `dot-stbl/stemma-docs` (чистый деплой).
Движок, хостинг, конкретный домен — обсуждается после MVP.

## Open questions (на момент 2026-08-13)

1. **Persistence:** нужна ли вообще? Если да — EF Core (один подход) или
   абстрактный `ICheckpointer<T>` + бэкенды (как у LangGraph)?
2. **Channels + reducers:** берём концепцию из LangGraph или .NET-native
   подход (например, типобезопасные редьюсеры через `IReducer<TState, TUpdate>`)?
3. **Subgraphs:** в каком виде? Вложенные `CompiledGraph` или композиция через
   `AddSubgraph(...)`?
4. **MAF integration:** отдельный пакет `StemmaGraph.MicrosoftAi` или часть
   основного `StemmaGraph`?
5. **Docs site:** движок (VitePress / Docusaurus / Astro Starlight), хостинг
   (Vercel / Cloudflare / GitHub Pages), конкретный домен.
6. **Public API tracking:** `Microsoft.CodeAnalysis.PublicApiAnalyzers` +
   `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` — добавлять после
   появления первой публичной поверхности.

## Связанное

- [`.agents/roadmap.md`](./roadmap.md) — что делаем дальше.
- [`.agents/conventions.md`](./conventions.md) — где живут конвенции.
- [`../CLAUDE.md`](../CLAUDE.md) — handbook.
- [`../AGENTS.md`](../AGENTS.md) — указатель для AI-агентов.
- Изначальная сессия обсуждения (notes): `stemma-notes.md` в temp-папке.