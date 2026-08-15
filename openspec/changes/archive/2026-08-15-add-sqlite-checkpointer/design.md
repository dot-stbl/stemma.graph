# Design: SQLite checkpointer

## Context
Existing durable providers (File, EF, S3) share: C-shape JSON payload, wire format
version 1, value allow-list, internal ctor + `Use*` DI, `IThreadDiscovery`.

## Goals / Non-Goals
- Goals: single-file SQLite store; Put/Get/List; thread discovery; conformance.
- Non-Goals: multi-writer production HA; Azure Blob / Redis; schema migrations tooling.

## Decisions
1. **Microsoft.Data.Sqlite** only (no EF in this package) — keeps the package light for demos.
2. **Table** `voluta_checkpoints (thread_id, step PK, status, payload_json)` — full snapshot JSON in payload.
3. **One long-lived connection** per singleton checkpointer, guarded by `SemaphoreSlim` (SQLite single-writer friendly).
4. **Wire types duplicated** under `Wire/` (same pattern as File/S3) — no shared wire assembly yet.

## Risks
- Concurrent host processes on one file: SQLite handles with busy timeout defaults; document single-process demos first.
