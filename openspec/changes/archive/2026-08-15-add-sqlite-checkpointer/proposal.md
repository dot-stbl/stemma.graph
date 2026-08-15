# Change: SQLite checkpointer provider

## Why
Laptop / single-node demos need a durable store that is one file, zero server, and
still passes the shared checkpointer conformance suite. File (JSON tree) exists;
SQLite is the natural next step for structured history + discovery without EF.

## What Changes
- New package `Voluta.Checkpoints.Sqlite` implementing `ICheckpointer` + `IThreadDiscovery`
- Wire format v1 (same allow-list / version rules as File / EF / S3)
- DI `UseSqlite(databasePath)` on the checkpoint builder
- Conformance + discovery + allow-list unit tests
- Docs matrix and main `checkpoint` spec updated for SQLite

## Capabilities
### New Capabilities
_(none)_

### Modified Capabilities
- `checkpoint`: add SQLite as a durable provider package with `UseSqlite`, wire v1, discovery

## Impact
- Affected code: `src/Voluta.Checkpoints.Sqlite`, tests, architecture isolation lists, README/docs
- Deferred (issue #76): Azure Blob, Redis/NATS — follow-up
