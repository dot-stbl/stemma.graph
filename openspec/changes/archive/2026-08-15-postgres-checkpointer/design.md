# Design: Postgres-native checkpointer

## Context

File / EF / S3 providers already share wire-format v1 JSON documents and
conformance. Issue #71 asks for a Postgres-native path optimized for ops.

## Goals / Non-Goals

- **Goals:** Put/Get/List + thread discovery; schema SQL + optional auto-ensure;
  `UsePostgres`; same allow-list as File/EF/S3; package isolation.
- **Non-Goals:** multi-tenant row security, partitioning, EF migrations for this
  package, shared wire library extraction (still duplicated per provider).

## Decisions

1. **Npgsql + raw SQL**, not EF — lighter deps; table is one JSONB row per step.
2. **PK `(thread_id, step)`** + `ON CONFLICT DO UPDATE` for idempotent Put.
3. **Identifier allow-list** for schema/table names (quoted SQL identifiers).
4. **`EnsureSchemaOnStartup` default true** — zero-config samples; ops can set
   false and apply embedded `Schema/voluta_checkpoints.sql`.
5. **Tests:** unit for wire/SQL always; live Postgres via env or Testcontainers
   (skip when Docker unavailable — agent-runtime-safe).

## Risks

- JSONB size for large channel graphs — same as File/S3 document size.
- Concurrent Puts on same (thread, step) last-write-wins via upsert.
