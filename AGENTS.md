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

Architecture locked in OpenSpec
`openspec/changes/architecture-runtime-core/` (no runtime code yet).
Decisions: [.agents/decisions.md](.agents/decisions.md). Roadmap:
[.agents/roadmap.md](.agents/roadmap.md). Conventions:
[.agents/conventions.md](.agents/conventions.md). GitHub epic #1 /
milestone `v0.1 · MVP runtime`.