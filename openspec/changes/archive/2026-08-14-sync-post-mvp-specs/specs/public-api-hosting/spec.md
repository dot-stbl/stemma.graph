## ADDED Requirements

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
- **WHEN** a graph is compiled with a host service provider and a node type is registered that type, and a node of that type is added by type
- **THEN** each invocation of that node can resolve the type from the context services and run successfully

#### Scenario: Missing services for typed node
- **WHEN** a typed node is registered but the graph is compiled without a host service provider
- **THEN** invoking the graph fails with an error that indicates services were not configured

### Requirement: DI-friendly node contract
The product MUST expose a node contract implementable as a class with constructor injection, registerable on the builder by type, by instance, or by factory from the host service provider.

#### Scenario: Add node by type
- **WHEN** a consumer registers a node type that implements the node contract and the type is present in host services
- **THEN** the graph invokes that implementation for the named node

### Requirement: Optional Agents AI package
Microsoft Extensions AI chat clients and Microsoft Agent Framework agents MUST integrate as optional package nodes that write assistant or agent text into channels, without adding AI package dependencies to the AOT core runtime packages.

#### Scenario: Chat client node writes channel
- **WHEN** a graph includes a chat-client node and the host provides a chat client (directly or via host services)
- **THEN** a successful completion writes assistant text to the configured output channel

#### Scenario: Core package stays free of AI dependencies
- **WHEN** a consumer references only the core runtime and abstractions packages
- **THEN** no Microsoft Agents AI or Microsoft Extensions AI packages are required to compile or run non-AI graphs
