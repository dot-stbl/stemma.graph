## Purpose

Defines the Voluta.Testing package and conformance expectations so checkpointer providers and runtime behavior can be validated uniformly without production-only tooling.

## ADDED Requirements

### Requirement: Testing package
The solution MUST include a `Voluta.Testing` package (or project published as such) that provides test doubles and helpers without requiring external databases.

#### Scenario: Unit test project reference
- **WHEN** a unit test project references `Voluta.Testing` and core runtime
- **THEN** it can construct recording checkpointers and graph fixtures without EF/S3 packages

### Requirement: Recording checkpointer
Testing helpers MUST provide a checkpointer that records Put/Get operations for assertions on order and payload shape.

#### Scenario: Assert checkpoint after interrupt
- **WHEN** a graph interrupts and tests use a recording checkpointer
- **THEN** the test can assert a Put occurred with interrupted status and payload

### Requirement: Fault-injecting checkpointer
Testing helpers MUST allow failing on the N-th Put (or similar) to simulate crash-between-supersteps and resume paths.

#### Scenario: Fail on second put
- **WHEN** fault injection is configured to throw on the second Put
- **THEN** a multi-superstep run surfaces the failure and a subsequent resume from the last successful Put can continue

### Requirement: Stream capture
Testing helpers MUST capture stream events for assertions independent of console/UI.

#### Scenario: Collect updates
- **WHEN** a test runs Stream with mode updates through a capturing observer
- **THEN** the test asserts node order and write contents from the captured list

### Requirement: Checkpoint conformance suite
A shared conformance suite MUST express the behavioral contract of `ICheckpointer` (roundtrip C-shape, missing thread, interrupt fields, pending writes when applicable) and MUST run against InMemory in CI.

#### Scenario: InMemory passes conformance
- **WHEN** the conformance suite executes against InMemory
- **THEN** all mandatory conformance tests pass in CI

#### Scenario: Future provider reuses suite
- **WHEN** an EF Core checkpointer package is added later
- **THEN** the same suite can be pointed at that provider without rewriting scenarios

### Requirement: Graph fixtures
Testing helpers MUST supply small graphs for linear, cycle, interrupt, and multi-ready (parallel) topologies for runtime tests.

#### Scenario: Parallel ready fixture
- **WHEN** tests use the multi-ready fixture
- **THEN** they can assert both nodes ran in one superstep without building topology ad hoc each time
