## MODIFIED Requirements

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
