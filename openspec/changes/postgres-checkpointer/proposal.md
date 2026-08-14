# Change: Postgres-native checkpointer

## Why

Production hosts often standardize on Postgres. Generic EF Core works but is
heavier and less opinionated for “just store checkpoints”. A first-class
Npgsql package gives ops a clear schema, connection string, and DI surface.

## What Changes

- New package `Voluta.Checkpoints.Postgres` (Npgsql, no EF).
- Table `public.voluta_checkpoints` (configurable schema/table) with JSONB
  wire-format v1 snapshots; optional `CREATE TABLE IF NOT EXISTS` on first use.
- `UsePostgres` on the checkpoint builder; Put/Get/List + `IThreadDiscovery`.
- Conformance + wire/allow-list unit tests (Testcontainers or `VOLUTA_TEST_PG`).
- Checkpoint spec delta: Postgres as a durable provider alongside File/EF/S3.

## Impact

- Affected specs: `checkpoint`
- Affected code: new package, slnx, architecture isolation tests, README
