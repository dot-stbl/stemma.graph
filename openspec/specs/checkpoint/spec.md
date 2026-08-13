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
