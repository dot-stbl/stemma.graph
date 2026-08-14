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

### Requirement: File EF and S3 provider packages
Durable checkpoint providers for local JSON files, EF Core (provider-agnostic relational), and S3-compatible object storage MUST ship as separate packages that implement the shared checkpointer contract and pass the same conformance suite as InMemory for put/get/list semantics.

#### Scenario: File provider roundtrip
- **WHEN** a consumer uses the file checkpointer package under a root directory
- **THEN** put and get for a thread preserve C-shape semantic content across process restarts on the same root

#### Scenario: EF Core provider with consumer DbContext factory
- **WHEN** a consumer registers a DbContext factory and configures the EF checkpointer against that context type
- **THEN** put and get work without the core package referencing EF Core

#### Scenario: S3 provider key layout
- **WHEN** a consumer configures an S3 checkpointer with bucket and key prefix
- **THEN** snapshots are stored under a stable key layout including thread and step and can be listed or fetched by thread

### Requirement: Wire format version on durable providers
File, EF Core, and S3 checkpoint payloads MUST include an explicit wire format version field on every write. Missing version on read MUST be treated as version 1. Unsupported future versions MUST fail with a stable checkpoint store error code rather than silent mis-parse.

#### Scenario: Write stamps version one
- **WHEN** a durable provider puts a checkpoint
- **THEN** the stored JSON document includes format version equal to 1

#### Scenario: Legacy document without version
- **WHEN** a durable provider reads a document that omits the format version field
- **THEN** the document is accepted as format version 1 and maps to a valid snapshot

#### Scenario: Unsupported version rejected
- **WHEN** a durable provider reads a document with format version greater than the supported version
- **THEN** the operation fails with a checkpoint store error identifying unsupported format version

### Requirement: Wire format v1 value allow-list on durable providers
File, EF Core, and S3 MUST reject channel values, pending write/send payloads, and interrupt payloads that are not in the wire format v1 allow-list at Put time, with stable code `checkpoint.unsupported_value_type`. Silent partial round-trip of arbitrary CLR graphs is forbidden.

Allow-listed shapes: `null`, string, bool, char, numeric primitives, `Guid`, date/time primitives (`DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan`), `JsonElement`, `byte[]`, lists/arrays of allow-listed values, and string-key dictionaries of allow-listed values (max nesting depth 8).

InMemory is process-local and MAY store arbitrary CLR references without JSON allow-list enforcement.

#### Scenario: Allow-listed values put successfully
- **WHEN** a durable provider puts a checkpoint whose values are only allow-listed shapes
- **THEN** Put succeeds and Get returns a snapshot with those channel keys present

#### Scenario: Unsupported CLR type rejected
- **WHEN** a durable provider puts a checkpoint containing a custom domain type (or Stream) as a channel value
- **THEN** Put fails with `CheckpointStoreException` code `checkpoint.unsupported_value_type` and does not persist the snapshot

### Requirement: Host construction via Use builders
Product documentation and DI fluent builders MUST treat File, EF Core, and S3 checkpointers as configured through the checkpoint builder (`UseFile` / `UseEntityFrameworkCore` / `UseS3`), not as the primary public construction path for hosts. Provider types may remain resolvable for advanced hosts, but public constructors of File/EF/S3 checkpointers MUST NOT be part of the supported host surface (internal or equivalent).

#### Scenario: DI UseFile registration
- **WHEN** the host configures checkpoints with UseFile and a root path
- **THEN** the checkpointer interface resolves to the file provider implementation

#### Scenario: InMemory remains newable
- **WHEN** a sample or test constructs the InMemory checkpointer with `new`
- **THEN** compile and runtime succeed without DI
