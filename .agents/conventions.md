# Conventions — где живут конвенции

> Указатель на все источники конвенций. Сами правила — в файлах по ссылкам.
> Не дублируем prose-правила тут — они расходятся с тем, что enforce'ит build.

## Single source of truth

| Что | Где | Приоритет |
| --- | --- | --- |
| **Code style (C#)** | [`.editorconfig`](../.editorconfig) | абсолютный — build падает на нарушении |
| **Build defaults (TFM, nullable, warnings-as-errors, package metadata)** | [`Directory.Build.props`](../Directory.Build.props) | абсолютный |
| **Package version policy (CPM off)** | [`Directory.Packages.props`](../Directory.Packages.props) | абсолютный |
| **Global rules (C#, process, observability)** | `~/.agents/rules/` (user-global) | ниже repo-local; repo-local overrides |
| **AI-agent workflow** | [`AGENTS.md`](../AGENTS.md), [`CLAUDE.md`](../CLAUDE.md) | handbook для AI |
| **Commit format** | `~/.agents/rules/process/commit-format.md` | абсолютный — `[voluta](feat/<area>): <subject>` |
| **Git hooks** | [`.githooks/`](../.githooks/) | абсолютный (commit-msg стрипает AI-атрибуцию) |
| **Line endings** | [`.gitattributes`](../.gitattributes) | абсолютный (LF enforced) |

## Style enforcement chain

```
.editorconfig          (severity error/warning на IDE/CA-правила)
    ↓
Directory.Build.props  (TreatWarningsAsErrors=true,
                        EnforceCodeStyleInBuild=true,
                        GenerateDocumentationFile=true)
    ↓
.NET SDK               (dotnet build прогоняет оба)
    ↓
0 warnings / 0 errors  (build gate)
```

**Следствие:** если `dotnet build voluta.slnx` зелёный — все
IDE/CA-правила выполнены. Точка. Никаких «CI поймает» — CI гоняет ту же
команду.

## Что НЕ покрыто автоматически

- Именование параметров (camelCase, без сокращений) — code review.
- Архитектурные границы пакетов — `tests/Voluta.Architecture.Unit`
  (csproj ProjectReference graph + NetArchTest type deps).
- Какие решения приняты и почему — [`decisions.md`](./decisions.md).
- Куда движемся — [`roadmap.md`](./roadmap.md).

## Когда обновлять

- `.editorconfig` — с одобрения владельца (см. правило `analyzers.md`
  в user-global, owner-approval на изменения severity).
- `Directory.Build.props` — с одобрения владельца (общая MSBuild-политика).
- `.agents/decisions.md` — на каждое решение **до** того, как оно стало кодом.
- `.agents/roadmap.md` — по мере выполнения.
- `.agents/adr/` — для обоснования спорных решений (отдельный ADR на
  каждое нетривиальное).