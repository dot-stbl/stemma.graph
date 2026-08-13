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

Scaffolding only — no runtime code yet. Architecture decisions live in
[CLAUDE.md](CLAUDE.md). Research on LangGraph internals in progress (see
`stemma-research.md` in temp dir).