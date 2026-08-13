# CLAUDE.md — StemmaGraph agent & developer handbook

> **AGENTS.md** — это короткий указатель на этот файл. Полные правила,
> архитектура и процедуры — здесь. AGENTS.md содержит только legacy-тезисы
> для совместимости с tooling, который ожидает именно его.

## Что такое StemmaGraph

Low-level orchestration framework для stateful-агентов в .NET. Концептуальный
источник — [LangGraph](https://github.com/langchain-ai/langgraph) (MIT), но
API .NET-native: generic state, типизированные редьюсеры, `IAsyncEnumerable`,
`Microsoft.Extensions.AI`.

**Не порт, не замена MAF.** Циклы + (опционально) checkpointing — то, что у MAF
нет и что StemmaGraph может дать.

## Текущий статус

**Architecture locked; runtime not implemented.** Scaffold packages only.
Canonical design: OpenSpec change
[`openspec/changes/architecture-runtime-core/`](openspec/changes/architecture-runtime-core/)
(`openspec validate architecture-runtime-core --strict`). Decisions:
[`.agents/decisions.md`](.agents/decisions.md) (D-001…D-021). Roadmap + GitHub
milestone `v0.1 · MVP runtime` (epic #1).

**MVP 0.1 (honest Pregel):** channels, C-shape checkpoint + InMemory, HITL
`NodeResult`, multi-mode stream, Both hosting, Testing package. **Not in 0.1:**
Send/subgraphs ship, EF/S3/File, UI (#13), MicrosoftAi.

LangGraph research notes (optional): temp `langgraph-explained.md` / clone.

## Tech stack

| Слой | Технология |
|------|------------|
| Runtime | .NET 10 |
| Тесты | xUnit + Shouldly + NSubstitute + Bogus |
| AI integration *(planned)* | `Microsoft.Extensions.AI` (`IChatClient`) |
| Streaming *(planned)* | `IAsyncEnumerable<T>` + `System.Threading.Channels` |
| Публикация | NuGet через GitHub Actions OIDC trusted publishing |

## Layout (текущий)

```
stemma.graph/
├── src/
│   ├── StemmaGraph/                  ← main runtime + builder (TBD)
│   └── StemmaGraph.Abstractions/     ← interfaces only, zero deps
├── samples/                          ← 01-HelloWorld (placeholder)
├── tests/                            ← smoke tests
├── .githooks/                        ← commit-msg strips AI attribution
├── .github/workflows/                ← ci.yml + publish.yml
└── stemma.graph.slnx
```

## Сборка и команды

```bash
# Build всего solution (warnings-as-errors)
dotnet build stemma.graph.slnx

# Tests
dotnet test stemma.graph.slnx

# Format gate (drift-check, --severity hidden для auto-fix)
dotnet format stemma.graph.slnx --severity hidden
```

**Build gate:** 0 warnings, 0 errors. `TreatWarningsAsErrors=true` +
`EnforceCodeStyleInBuild=true` в `Directory.Build.props` ловят всё
автоматически.

## Конвенции

Style enforced автоматически — большая часть **не** записана в prose:

- **`.editorconfig`** (repo root) — severity error/warning, плюс analyzers
  (`Microsoft.CodeAnalysis.NetAnalyzers`, `Microsoft.VisualStudio.Threading.Analyzers`,
  source-link).

Quick reference (конвенции, которые build enforce'ит):

- Match neighboring code style.
- Block-bodied members only (no expression-bodied methods); explicit access modifiers.
- **NO** underscore prefix for private fields; **NO** single-letter lambda parameters.
- **ВСЕГДА** primary constructors + Pyramid Rule (short → long parameter ordering).
- **Default** to `sealed class`, not `record` (records only for value objects / DTO).
- Pattern matching: `is { }` / `is not { }` for null checks.
- **NO** `.Result` / `.Wait()` / `.GetAwaiter().GetResult()`.
- **NO** private business-logic methods — use services or `file static` helpers.
- DI-регистрация: руками через installer-классы. Без reflection auto-reg.

## NuGet-публикация

GitHub Actions OIDC → nuget.org trusted publishing. **Без долгоживущих
API-ключей.** Workflow: `.github/workflows/publish.yml` — триггер на тег
`v*.*.*`, после успешного CI. Детали — в workflow.

## Communication

Русский — основной. Документация для пользователей (README, доки на сайте) —
на английском, потому что OSS-аудитория международная.

## Где что лежит

**Корень** — публичные точки входа и общие конфиги:

- README.md — публичный обзор, quick start, roadmap.
- CLAUDE.md — **этот файл**: внутренняя кухня, конвенции, workflow.
- AGENTS.md — короткий указатель на CLAUDE.md для AI-агентов.
- CONTRIBUTING.md — как контрибьютить (setup, build, tests, commit).
- LICENSE — MIT.
- .editorconfig / Directory.Build.props / Directory.Packages.props —
  enforced стиль и build defaults.
- .gitignore / .gitattributes — git hygiene.
- .githooks/ — commit-msg (AI attribution strip), pre-commit (no-op).
- assets/ — графика:
  - `banner.png` (2400×600) — README header, dither-brutalism; не ресайзить.
  - `package-icon.png` — NuGet icon (из `icon-i5.png`); `PackageIcon` в
    `Directory.Build.props`.
  - `icon-i1.png` / `icon-i4.png` / `icon-i5.png` — варианты иконок.
- .github/workflows/ — CI + publish.
- stemma.graph.slnx — solution.

**`.agents/`** — внутренние документы (не публикуются на NuGet, не
рендерятся в README):

- `.agents/decisions.md` — принятые решения + open questions.
- `.agents/roadmap.md` — порядок работ.
- `.agents/conventions.md` — где живут конвенции (single source of truth).
- `.agents/adr/` — ADR (architecture decision records) для нетривиальных решений.
- `.agents/research/` — внешние исследования (LangGraph internals и т.п.).

## Связанное

- [`.agents/decisions.md`](.agents/decisions.md) — принятые решения.
- [`.agents/roadmap.md`](.agents/roadmap.md) — что делаем дальше.
- [`.agents/conventions.md`](.agents/conventions.md) — где живут конвенции.
- [`.agents/research/`](.agents/research/) — внешние исследования.
- LangGraph research notes (background agent, ещё в процессе):
  `C:/Users/bradw/AppData/Local/Temp/opencode/stemma-research.md`.
- `~/.agents/rules/` в user-global — общие C#/process правила (приоритет ниже
  repo-local).