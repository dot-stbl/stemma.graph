# Cross-thread Store Specification

## Purpose

Defines a host-scoped key-value store shared across graph run threads
(LangGraph-class BaseStore parity). Distinct from per-thread C-shape
checkpoints (`ICheckpointer`).

## Deferred (out of this capability slice)

- Subgraph stream namespaces (parent observes child streams cleanly)
- Task journal (A3) — completed task ids for stronger Continue
- Durable store providers (File / EF / S3) and wire-format value allow-list
- Checkpoint schema migration across format versions
- Vector / semantic search over store entries

## Requirements

### Requirement: Pluggable IVolutaStore

The abstractions package MUST expose `IVolutaStore` with Put / Get / List / Delete
over a hierarchical namespace + string key. Runtime MUST NOT couple graph execution
to a concrete store technology beyond the built-in InMemory provider.

#### Scenario: Host resolves store in a node

- **WHEN** the host registers `IVolutaStore` and compiles with `CompileOptions.Services`
- **THEN** a node can resolve the store via `GraphContext.GetRequiredService<IVolutaStore>()`
  and Put/Get without using the thread id as the isolation key

### Requirement: InMemory provider in core

The core library MUST ship `InMemoryVolutaStore` suitable for tests and single-process
samples, registered as a normal DI singleton (not a special-case engine path).

#### Scenario: Sample without external store

- **WHEN** a host calls `AddVoluta(v => v.Store.UseInMemory())` or `AddVolutaStore(s => s.UseInMemory())`
- **THEN** Put / Get / List / Delete work within one process lifetime across concurrent threads

### Requirement: Namespace + key addressing

Each item is addressed by an ordered list of namespace segments plus a key. Empty
namespace is the root. List MUST return only exact-namespace matches (not prefix),
ordered by key ascending (ordinal).

#### Scenario: List isolation

- **WHEN** items exist under `["users","memories"]` and under `["other"]`
- **THEN** `ListAsync(["users","memories"])` returns only the first namespace’s keys

### Requirement: Get miss and Delete miss

Get for an unknown key MUST return null and MUST NOT throw. Delete for an unknown
key MUST be a no-op and MUST NOT throw. Storage outages on durable providers MAY throw
a documented exception in a later slice.

#### Scenario: Unknown key

- **WHEN** the host calls Get on a key that was never Put
- **THEN** the result is null without treating it as a storage outage

### Requirement: Optional relative to checkpoints

Store registration MUST be independent of `ICheckpointer`. A host MAY register only
checkpoints, only store, both, or neither.

#### Scenario: Store without checkpointer

- **WHEN** the host calls `AddVolutaStore(s => s.UseInMemory())` without `Use*` checkpoints
- **THEN** `IVolutaStore` resolves and Put/Get succeed
