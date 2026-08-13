# Contributing to StemmaGraph

> **TL;DR:** clone → `dotnet build stemma.graph.slnx` → правь код →
> `dotnet build` + `dotnet test` → commit (`[stemma](feat/...): ...`) →
> push → PR.

---

## 1. Prerequisites

| Tool | Version | Why |
|------|---------|-----|
| **.NET SDK** | 10.0.100+ | Build, test, pack |
| **Git** | 2.30+ | VCS |

```bash
dotnet --version    # >= 10.0.100
```

**IDE:** JetBrains Rider (recommended), Visual Studio 2022 17.12+, or VS Code
with C# Dev Kit.

## 2. Initial Setup

```bash
git clone https://github.com/dot-stbl/stemma.graph.git
cd stemma.graph

# Git hooks (commit-msg strips AI attribution, pre-commit is no-op for now)
git config core.hooksPath .githooks

# Restore
dotnet restore stemma.graph.slnx
```

### Verify setup

```bash
dotnet build stemma.graph.slnx    # 0 warnings / 0 errors
dotnet test  stemma.graph.slnx    # all tests pass
```

## 3. Daily Workflow

### 3.1 Branch naming

Branch off `main`:

```bash
git checkout main && git pull
git checkout -b feature/<short-description>
# or: fix/, docs/, test/, refactor/, chore/
```

Examples: `feature/state-graph-builder`, `fix/naming-violation`,
`docs/readme-quickstart`.

### 3.2 Code → Test → Lint → Commit

```bash
dotnet build stemma.graph.slnx    # 0/0 — обязательно
dotnet test  stemma.graph.slnx    # все тесты зелёные
git add <files>
git commit -m "[stemma](feat/<area>): <subject>"
```

The `commit-msg` hook auto-strips AI co-author trailers (Claude, GPT, etc.)
from the commit message. Legitimate human co-authors are kept.

### 3.3 Push → PR

```bash
git push -u origin feature/<name>
# Open PR against main
```

## 4. Building

```bash
dotnet build stemma.graph.slnx                       # Debug
dotnet build stemma.graph.slnx -c Release --nologo   # Release
```

**Requirement: 0 warnings / 0 errors.** `TreatWarningsAsErrors=true` +
`EnforceCodeStyleInBuild=true` in `Directory.Build.props` enforce this — a
clean build means the rules pass. The rules themselves live in `.editorconfig`.

## 5. Testing

```bash
dotnet test stemma.graph.slnx                              # everything
dotnet test --filter "FullyQualifiedName~StateGraph"       # focused
```

**Stack:** xUnit + Shouldly + NSubstitute + Bogus. Integration test project
exists but has no real scenarios yet (those land alongside the runtime).

## 6. Commit Message Convention

Format: `[stemma](feat/<area>): <subject>`

| Prefix | When |
|--------|------|
| `feat` | New feature |
| `fix` | Bug fix |
| `refactor` | Refactor without behaviour change |
| `docs` | Documentation only |
| `test` | Tests only |
| `chore` | Build / tooling / deps |

```
[stemma](feat/core): add StateGraph builder with fluent API
[stemma](fix/checkpoint): resolve SQLite WAL mode on Windows
[stemma](refactor): extract IReducer<T> from internal channel logic
```

**Forbidden** in commit messages (and stripped by `commit-msg` hook):
`Co-Authored-By: Claude` / `Generated with ...` / `assisted by ...`.

## 7. Pull Request Process

1. **Title:** short description (e.g. `feat(core): StateGraph fluent builder`).
2. **Description:** what + why + how verified (build 0/0, tests passing).
3. **CI checks:** all green (build, tests, lint).
4. **Reviewers:** 1 approval minimum.
5. **Merge:** squash-merge; delete branch after merge.

## 8. Architecture

See [CLAUDE.md](CLAUDE.md) for the architecture discussion, conventions,
and module layout.

## 9. Troubleshooting

### 9.1 `dotnet build` fails immediately
NuGet restore not run: `dotnet restore stemma.graph.slnx`.

### 9.2 Hooks not firing
Check `git config core.hooksPath` → should be `.githooks`. On Windows, hooks
require `sh` (Git Bash).

---

## 10. Related

- [README.md](README.md) — project overview.
- [CLAUDE.md](CLAUDE.md) — architecture, conventions, workflow.
- [.editorconfig](.editorconfig) — enforced style rules.
- [LICENSE](LICENSE) — MIT.