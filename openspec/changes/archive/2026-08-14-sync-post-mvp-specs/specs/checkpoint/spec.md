## ADDED Requirements

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

### Requirement: Host construction via Use builders
Product documentation and DI fluent builders MUST treat File, EF Core, and S3 checkpointers as configured through the checkpoint builder (`UseFile` / `UseEntityFrameworkCore` / `UseS3`), not as the primary public construction path for hosts. Provider types may remain resolvable for advanced hosts, but public constructors of File/EF/S3 checkpointers MUST NOT be part of the supported host surface (internal or equivalent).

#### Scenario: DI UseFile registration
- **WHEN** the host configures checkpoints with UseFile and a root path
- **THEN** the checkpointer interface resolves to the file provider implementation

#### Scenario: InMemory remains newable
- **WHEN** a sample or test constructs the InMemory checkpointer with `new`
- **THEN** compile and runtime succeed without DI
