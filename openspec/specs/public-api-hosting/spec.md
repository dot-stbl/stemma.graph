# Public Api Hosting Specification

## Purpose

Defines the public builder/compile/run surface and dual hosting model: standalone fluent use and DI registration without two different runtimes.

## Requirements

### Requirement: Builder compiles to immutable graph
Consumers MUST build a graph (nodes, edges, channels) and compile it into an immutable runnable graph. Mutation of topology after compile MUST NOT affect the runnable instance.

#### Scenario: Compile once
- **WHEN** a graph is compiled and then the builder is further mutated
- **THEN** the already-compiled instance retains the pre-mutation topology

### Requirement: Standalone fluent hosting
Consumers MUST be able to construct, compile, invoke, stream, and resume a graph without a DI container.

#### Scenario: Console sample
- **WHEN** a sample uses only `new` + fluent builder + InMemory checkpointer
- **THEN** a full ReAct-style loop can run without `IServiceProvider`

### Requirement: DI hosting of compiled graph
Consumers MUST be able to register a compiled graph (or factory) in DI and resolve a runner façade for invoke/stream/resume.

#### Scenario: ASP.NET endpoint
- **WHEN** the host registers a compiled graph as a singleton and injects a runner into an endpoint
- **THEN** concurrent requests can invoke with different thread ids against the same compiled topology

### Requirement: Compile-once lifetime
Documentation and APIs MUST treat compile as expensive: the supported product pattern is compile at startup (or once per topology version), invoke many times per request/job.

#### Scenario: Per-request compile discouraged
- **WHEN** a consumer compiles a new graph on every HTTP request
- **THEN** the library still functions but docs mark this as unsupported for production performance

### Requirement: Thread isolation
Runs MUST be isolated by thread (conversation/run) id so concurrent invokes do not share mutable channel state across threads.

#### Scenario: Two threads
- **WHEN** two invokes use different thread ids against the same compiled graph
- **THEN** checkpoints and channel values do not leak between those threads
