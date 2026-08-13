# AGENTS.md

This file provides guidance to agents when working with code in this repository.

> **Primary agent doc — [CLAUDE.md](CLAUDE.md).** Это тонкий указатель;
> архитектура, конвенции и workflow живут там.

## Build & test

```bash
dotnet build stemma.graph.slnx   # 0 warnings / 0 errors (warnings-as-errors)
dotnet test  stemma.graph.slnx   # xUnit + Shouldly + NSubstitute + Bogus
```

## Current status

**Runtime + post-MVP packages on `main`.** Specs live in
[`openspec/specs/`](openspec/specs/) (12 capabilities). Architecture change
archived as
`openspec/changes/archive/2026-08-14-architecture-runtime-core/`.
Decisions: [.agents/decisions.md](.agents/decisions.md). Roadmap:
[.agents/roadmap.md](.agents/roadmap.md). Conventions:
[.agents/conventions.md](.agents/conventions.md). GitHub epic #1 /
milestone `v0.1 · MVP runtime` (NuGet tag still open).
