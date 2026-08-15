## MODIFIED Requirements

### Requirement: File EF S3 and Postgres provider packages
Durable checkpoint providers for local JSON files, SQLite single-file stores, EF Core (provider-agnostic relational), S3-compatible object storage, and Postgres-native storage MUST ship as separate packages that implement the shared checkpointer contract and pass the same conformance suite as InMemory for put/get/list semantics.

#### Scenario: File provider roundtrip
- **WHEN** a consumer uses the file checkpointer package under a root directory
- **THEN** put and get for a thread preserve C-shape semantic content across process restarts on the same root

#### Scenario: SQLite provider roundtrip
- **WHEN** a consumer uses the SQLite checkpointer package with a database file path
- **THEN** put and get for a thread preserve C-shape semantic content across process restarts on the same file

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

### Requirement: Host construction via Use builders
Product documentation and DI fluent builders MUST treat File, SQLite, EF Core, S3, and Postgres checkpointers as configured through the checkpoint builder (`UseFile` / `UseSqlite` / `UseEntityFrameworkCore` / `UseS3` / `UsePostgres`), not as the primary public construction path for hosts. Provider types may remain resolvable for advanced hosts, but public constructors of File/SQLite/EF/S3/Postgres checkpointers MUST NOT be part of the supported host surface (internal or equivalent).

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
