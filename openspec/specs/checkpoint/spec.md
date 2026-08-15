# Checkpoint Specification

## Purpose

Defines durable C-shape checkpoints, the checkpointer abstraction, InMemory as the built-in provider, and the package map for external storage backends.
## Requirements
### Requirement: Full C-shape snapshot
A checkpoint MUST capture at least: channel values, channel versions, versions_seen (per node), pending writes (if any incomplete tasks), step index, run status, and optional interrupt payload.

#### Scenario: Roundtrip preserves C fields
- **WHEN** a checkpoint with versions, versions_seen, and pending writes is Put then Get for the same thread
- **THEN** all C-shape fields roundtrip without loss of semantic content

### Requirement: Checkpoint after successful superstep
The runtime SHALL persist a checkpoint after a superstep’s writes are applied (and before the next superstep begins), when a checkpointer is configured.

#### Scenario: Crash between supersteps
- **WHEN** a run completes superstep N and checkpoints, then the process dies before superstep N+1
- **THEN** resume loads step N state and continues from the ready set derived from that checkpoint

### Requirement: Pluggable ICheckpointer
Storage backends MUST implement a shared checkpointer contract (put/get/list-or-equivalent). Runtime MUST NOT depend on a concrete storage technology beyond the built-in InMemory provider.

#### Scenario: Swap provider
- **WHEN** compile is given an EF Core (or other) checkpointer instead of InMemory
- **THEN** the same graph run API works without graph definition changes

### Requirement: InMemory provider in core
The core library MUST ship an InMemory checkpointer suitable for tests and single-process samples, registered as a normal provider (not a special-case code path).

#### Scenario: Sample without external store
- **WHEN** a sample compiles with InMemory only
- **THEN** invoke, interrupt, and resume work within one process lifetime

### Requirement: Mid-superstep pending writes
When a superstep is interrupted by process failure after some tasks complete, a conforming checkpointer and runtime MUST be able to store and restore pending writes so completed tasks are not blindly re-executed.

#### Scenario: Partial parallel completion
- **WHEN** two parallel tasks run and only one finishes before a simulated crash with pending writes saved
- **THEN** resume re-runs only incomplete tasks and does not duplicate the finished task’s side-effecting write application

### Requirement: Provider packages
External providers (EF Core, S3/blob, File, optional graph DB) MUST live in separate packages so the core package does not take their dependencies transitively.

#### Scenario: Core package dependencies
- **WHEN** a consumer references only the core runtime package
- **THEN** no EF Core, AWS, or file-provider packages are required to compile or run InMemory scenarios

### Requirement: Get missing thread
Get for an unknown thread id MUST return a documented empty result (null/none) and MUST NOT throw as a normal miss.

#### Scenario: Unknown thread
- **WHEN** the host calls Get on a thread id that was never Put
- **THEN** the result indicates not found without treating it as a storage outage

### Requirement: Optional list history
A checkpointer MAY support listing checkpoints for a thread (time-travel). If unsupported, the API MUST fail clearly or return a not-supported result rather than partial silent data.

#### Scenario: InMemory list
- **WHEN** InMemory has stored multiple steps for a thread and list is supported
- **THEN** the host can enumerate checkpoints ordered by step

### Requirement: Optional thread discovery
A checkpointer MAY implement `IThreadDiscovery` to enumerate known thread identifiers across the store (not only history for one thread). Ops UI and multi-host tooling cast the registered `ICheckpointer` to `IThreadDiscovery` when available. An empty store MUST return an empty list; storage outages may throw `CheckpointStoreException`.

#### Scenario: File root scan
- **WHEN** File checkpointer has put checkpoints for threads A and B under a root directory
- **THEN** a new process with the same root can list both thread ids via `ListThreadIdsAsync`

#### Scenario: UI merge after restart
- **WHEN** the ops UI session has no in-process tracked ids and the checkpointer implements discovery with durable threads
- **THEN** `ListThreadsAsync` returns those threads with latest status from Get

### Requirement: Host time-travel read façade
The compiled graph MUST expose host-facing time-travel reads that wrap checkpointer get/list without leaking engine-only C-shape fields as the primary product surface.

- `GetStateAsync(threadId)` MUST return a `ThreadSnapshot` for the latest checkpoint, or a documented empty result (null) when the thread was never put.
- `GetHistoryAsync(threadId)` MUST return ordered `ThreadSnapshot` steps (oldest first) when the provider supports list; providers that do not support list MUST throw `NotSupportedException` (or equivalent) rather than return partial silent data.
- `ThreadSnapshot` MUST include at least: thread id, step, status, channel values, last node, next nodes, and interrupt payload when present.

#### Scenario: GetState after done
- **WHEN** a thread has completed successfully and the host calls `GetStateAsync`
- **THEN** the result has status Done and channel values matching the latest checkpoint

#### Scenario: GetHistory ordered
- **WHEN** a thread has multiple checkpoints and the host calls `GetHistoryAsync`
- **THEN** steps are ordered ascending and the last entry matches `GetStateAsync`

#### Scenario: Missing thread GetState
- **WHEN** the host calls `GetStateAsync` for a thread that was never put
- **THEN** the result indicates not found without treating it as a storage outage

### Requirement: Host update state and fork
The compiled graph MUST expose host-facing mutation APIs that apply channel writes and branch threads without reopening the engine-only C-shape surface.

- `UpdateStateAsync(threadId, writes)` MUST load the latest checkpoint, apply writes through the same channel reducers as runtime input seeding (LastValue / Append / etc.), and Put a **new** history step at `latest.Step + 1` with updated channel values and versions.
- Status policy after update: `Failed` / `Cancelled` become `Running` (so continue can re-drive); `Interrupted` stays `Interrupted` (resume via command); `Done` / `Running` keep their status; NextNodes / interrupt payload / pending sends are preserved when status remains non-terminal.
- `ForkAsync(sourceThreadId, step, newThreadId)` MUST list source history, copy the snapshot at the requested step onto `newThreadId` **keeping the same step index** as the fork root, and leave the source thread unchanged. Missing source thread → stable `graph.thread_not_found`; missing step → `graph.step_not_found`.
- `ContinueAsync` / `ContinueInvokeAsync` MUST re-enter the superstep loop from a **Running** latest checkpoint (NextNodes and/or pending sends). Interrupted threads MUST use `ResumeAsync` instead. Side-effect re-execution of nodes in NextNodes is host responsibility (document; no automatic dedup).

#### Scenario: Update then resume interrupt
- **WHEN** a thread is Interrupted and the host calls `UpdateStateAsync` with append writes then `ResumeInvokeAsync`
- **THEN** the patched channel values are visible after resume and the run can complete

#### Scenario: Fork preserves source independence
- **WHEN** the host forks step N to a new thread and updates only the new thread
- **THEN** the source thread history does not contain the new thread’s writes

#### Scenario: Missing step on fork
- **WHEN** the host forks a step that does not exist on the source thread
- **THEN** the operation fails with `graph.step_not_found`

#### Scenario: Continue after fork Running
- **WHEN** the host forks a Running checkpoint with next nodes and calls `ContinueInvokeAsync` on the new thread
- **THEN** the run continues and can reach Done

### Requirement: File EF S3 and Postgres provider packages
Durable checkpoint providers for local JSON files, EF Core (provider-agnostic
relational), S3-compatible object storage, Postgres-native storage, and SQLite
single-file stores MUST ship as separate packages that implement the shared
checkpointer contract and pass the same conformance suite as InMemory for
put/get/list semantics.

#### Scenario: File provider roundtrip
- **WHEN** a consumer uses the file checkpointer package under a root directory
- **THEN** put and get for a thread preserve C-shape semantic content across process
  restarts on the same root

#### Scenario: SQLite provider roundtrip
- **WHEN** a consumer uses the SQLite checkpointer package with a database file path
- **THEN** put and get for a thread preserve C-shape semantic content across process
  restarts on the same file

#### Scenario: EF Core provider with consumer DbContext factory
- **WHEN** a consumer registers a DbContext factory and configures the EF checkpointer
  against that context type
- **THEN** put and get work without the core package referencing EF Core

#### Scenario: S3 provider key layout
- **WHEN** a consumer configures an S3 checkpointer with bucket and key prefix
- **THEN** snapshots are stored under a stable key layout including thread and step
  and can be listed or fetched by thread

#### Scenario: Postgres provider table layout
- **WHEN** a consumer configures a Postgres checkpointer with connection string
- **THEN** snapshots are stored as rows keyed by thread and step with a JSONB snapshot
  payload and can be listed or fetched by thread

### Requirement: Postgres-native provider package
A Postgres-native checkpointer package MUST ship separately from the core runtime
and from the provider-agnostic EF Core package. It MUST implement the shared
checkpointer contract (Put/Get/List) and MAY implement `IThreadDiscovery`.
Snapshots MUST use the same wire-format v1 JSON document and value allow-list as
File, EF Core, and S3. Storage MUST use a relational table with primary key
`(thread_id, step)` and a JSONB (or equivalent) column for the snapshot document.
Hosts MUST register the provider through the checkpoint builder (`UsePostgres`),
not by relying on a public constructor as the supported host surface.

#### Scenario: Postgres provider roundtrip
- **WHEN** a consumer configures `UsePostgres` with a connection string and puts
  a checkpoint
- **THEN** get for the same thread returns a snapshot preserving C-shape semantic
  content

#### Scenario: Postgres thread discovery
- **WHEN** checkpoints exist for threads A and B in the configured table
- **THEN** `ListThreadIdsAsync` returns both thread identifiers

#### Scenario: Schema bootstrap
- **WHEN** `EnsureSchemaOnStartup` is true and the table is missing
- **THEN** the provider creates the table (or equivalent) before the first put
  without requiring a separate migration step

#### Scenario: DI UsePostgres registration
- **WHEN** the host configures checkpoints with UsePostgres and a connection string
- **THEN** the checkpointer interface resolves to the Postgres provider implementation

### Requirement: Wire format version on durable providers
File, EF Core, S3, and Postgres checkpoint payloads MUST include an explicit wire
format version field on every write. Missing version on read MUST be treated as
version 1. Unsupported future versions MUST fail with a stable checkpoint store
error code rather than silent mis-parse.

#### Scenario: Write stamps version one
- **WHEN** a durable provider puts a checkpoint
- **THEN** the stored JSON document includes format version equal to 1

#### Scenario: Legacy document without version
- **WHEN** a durable provider reads a document that omits the format version field
- **THEN** the document is accepted as format version 1 and maps to a valid snapshot

#### Scenario: Unsupported version rejected
- **WHEN** a durable provider reads a document with format version greater than the
  supported version
- **THEN** the operation fails with a checkpoint store error identifying unsupported
  format version

### Requirement: Wire format v1 value allow-list on durable providers
File, EF Core, S3, and Postgres MUST reject channel values, pending write/send
payloads, and interrupt payloads that are not in the wire format v1 allow-list at
Put time, with stable code `checkpoint.unsupported_value_type`. Silent partial
round-trip of arbitrary CLR graphs is forbidden.

Allow-listed shapes: `null`, string, bool, char, numeric primitives, `Guid`,
date/time primitives (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`,
`TimeSpan`), `JsonElement`, `byte[]`, lists/arrays of allow-listed values, and
string-key dictionaries of allow-listed values (max nesting depth 8).

InMemory is process-local and MAY store arbitrary CLR references without JSON
allow-list enforcement.

#### Scenario: Allow-listed values put successfully
- **WHEN** a durable provider puts a checkpoint whose values are only allow-listed shapes
- **THEN** Put succeeds and Get returns a snapshot with those channel keys present

#### Scenario: Unsupported CLR type rejected
- **WHEN** a durable provider puts a checkpoint containing a custom domain type (or Stream)
  as a channel value
- **THEN** Put fails with `CheckpointStoreException` code `checkpoint.unsupported_value_type`
  and does not persist the snapshot

### Requirement: Host construction via Use builders
Product documentation and DI fluent builders MUST treat File, EF Core, S3, and
Postgres checkpointers as configured through the checkpoint builder (`UseFile` /
`UseEntityFrameworkCore` / `UseS3` / `UsePostgres`), not as the primary public
construction path for hosts. Provider types may remain resolvable for advanced
hosts, but public constructors of File/EF/S3/Postgres checkpointers MUST NOT be
part of the supported host surface (internal or equivalent).

#### Scenario: DI UseFile registration
- **WHEN** the host configures checkpoints with UseFile and a root path
- **THEN** the checkpointer interface resolves to the file provider implementation

#### Scenario: DI UseSqlite registration
- **WHEN** the host configures checkpoints with UseSqlite and a database path
- **THEN** the checkpointer interface resolves to the SQLite provider implementation

#### Scenario: DI UsePostgres registration
- **WHEN** the host configures checkpoints with UsePostgres and a connection string
- **THEN** the checkpointer interface resolves to the Postgres provider implementation

#### Scenario: InMemory remains newable
- **WHEN** a sample or test constructs the InMemory checkpointer with `new`
- **THEN** compile and runtime succeed without DI

### Requirement: Incomplete-only Continue ready set
When `ContinueAsync` loads a Running checkpoint that has non-empty `PendingSends`, the runtime MUST schedule those push tasks and MUST NOT re-drive `NextNodes` as fresh pull tasks in the same continue entry. This avoids re-executing side-effect nodes that already completed and only scheduled Sends. When `PendingSends` is empty, Continue MAY pull from `NextNodes` (fork/update of a pull barrier).

#### Scenario: Continue pending sends skips map re-pull
- **WHEN** a Running checkpoint has PendingSends for workers and NextNodes still lists a completed map node
- **THEN** Continue runs workers only and does not re-invoke the map node

