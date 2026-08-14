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

### Requirement: Unified AddVoluta composition root
Hosts MUST be able to register checkpoints and a compiled graph through a single composition-root API that configures a nested checkpoint builder and a graph factory, without requiring separate ad-hoc singleton registrations for the common case.

#### Scenario: Checkpoints and graph in one registration
- **WHEN** the host configures in-memory (or other) checkpoints and a graph factory that receives the resolved checkpointer
- **THEN** the process can resolve both the checkpointer and the compiled graph as singletons

#### Scenario: Graph factory requiring checkpoints without Use
- **WHEN** the host registers a graph factory that depends on a resolved checkpointer but does not configure any checkpoint provider
- **THEN** registration fails at composition time with a clear configuration error

### Requirement: Graph host services on compile
Compilation MUST accept an optional host service provider that is exposed on every node invocation context for the life of the compiled graph.

#### Scenario: Typed node resolves dependency
- **WHEN** a graph is compiled with a host service provider and a node type is registered in that provider, and a node of that type is added by type
- **THEN** each invocation of that node can resolve the type from the context services and run successfully

#### Scenario: Missing services for typed node
- **WHEN** a typed node is registered but the graph is compiled without a host service provider
- **THEN** invoking the graph fails with an error that indicates services were not configured

### Requirement: DI-friendly node contract
The product MUST expose a node contract implementable as a class with constructor injection, registerable on the builder by type, by instance, or by factory from the host service provider.

#### Scenario: Add node by type
- **WHEN** a consumer registers a node type that implements the node contract and the type is present in host services
- **THEN** the graph invokes that implementation for the named node

### Requirement: Time-travel read on CompiledGraph
Standalone and DI hosts MUST be able to inspect current state and step history through `CompiledGraph.GetStateAsync` / `GetHistoryAsync` without opening the checkpointer directly. Ops UI MAY surface the same history via an HTTP endpoint when list is supported.

#### Scenario: Host reads state after invoke
- **WHEN** a host invokes a graph and then calls `GetStateAsync` with the same thread id
- **THEN** the returned `ThreadSnapshot` reflects the latest checkpoint status and values

### Requirement: Optional Agents AI package
Microsoft Extensions AI chat clients and Microsoft Agent Framework agents MUST integrate as optional package nodes that write assistant or agent text into channels, without adding AI package dependencies to the AOT core runtime packages.

#### Scenario: Chat client node writes channel
- **WHEN** a graph includes a chat-client node and the host provides a chat client (directly or via host services)
- **THEN** a successful completion writes assistant text to the configured output channel

#### Scenario: Core package stays free of AI dependencies
- **WHEN** a consumer references only the core runtime and abstractions packages
- **THEN** no Microsoft Agents AI or Microsoft Extensions AI packages are required to compile or run non-AI graphs
