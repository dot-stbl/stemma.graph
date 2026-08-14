## Why

Main OpenSpec specs still describe the early MVP surface. Post-MVP code on `main`
(graph DI / `IGraphNode`, `AddVoluta` composition root, `Voluta.Agents.AI`,
checkpoint wire `formatVersion`, internalized provider ctors, architecture tests)
is documented in README / decisions / PublicAPI review but **not** in
`openspec/specs/*`. Specs are the behavioral source of truth (D-021) — they must
catch up before the `v0.1.0` freeze so validation and future deltas stay honest.

## What Changes

- Bring main specs in line with **already shipped** behavior (no new product code).
- Extend **public-api-hosting** for `AddVoluta` / `VolutaBuilder`, `CompileOptions.Services`,
  `IGraphNode` / `AddNode<T>`, and the Agents.AI package role.
- Extend **checkpoint** for File / EF / S3 providers, wire `formatVersion`, and
  host construction via `Use*` (public ctors internalized for providers).
- Extend **quality-engineering** for architecture tests and PublicAPI ship gate.
- Optionally note MEAI/MAF integration boundary (no own LLM SDK) under public-api-hosting
  or a thin new capability — prefer **modified** public-api-hosting to avoid package sprawl.

No **BREAKING** product API changes in this change — documentation/spec only.

## Capabilities

### New Capabilities

_(none — all behavior maps to existing capabilities)_

### Modified Capabilities

- `public-api-hosting`: DI composition root (`AddVoluta`), graph DI (`Services` /
  `IGraphNode`), optional `Voluta.Agents.AI` package
- `checkpoint`: provider packages + wire format version + construction via `Use*`
- `quality-engineering`: architecture tests present; PublicAPI analyzer gate

## Impact

- Files: `openspec/specs/{public-api-hosting,checkpoint,quality-engineering}/spec.md`
  (after sync); this change’s deltas under `openspec/changes/sync-post-mvp-specs/`
- No runtime package code changes required
- Unblocks honest `v0.1.0` tagging against specs that match the shipped surface
