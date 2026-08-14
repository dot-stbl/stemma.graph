## MODIFIED Requirements

### Requirement: File SQLite EF and S3 provider packages
Durable checkpoint providers for local JSON files, SQLite single-file stores, EF Core (provider-agnostic relational), and S3-compatible object storage MUST ship as separate packages that implement the shared checkpointer contract and pass the same conformance suite as InMemory for put/get/list semantics.

#### Scenario: SQLite provider roundtrip
- **WHEN** a consumer uses the SQLite checkpointer package with a database file path
- **THEN** put and get for a thread preserve C-shape semantic content across process restarts on the same file

### Requirement: Host construction via Use builders
Product documentation and DI fluent builders MUST treat File, SQLite, EF Core, and S3 checkpointers as configured through the checkpoint builder (`UseFile` / `UseSqlite` / `UseEntityFrameworkCore` / `UseS3`), not as the primary public construction path for hosts. Provider types may remain resolvable for advanced hosts, but public constructors of File/SQLite/EF/S3 checkpointers MUST NOT be part of the supported host surface (internal or equivalent).

#### Scenario: DI UseSqlite registration
- **WHEN** the host configures checkpoints with UseSqlite and a database path
- **THEN** the checkpointer interface resolves to the SQLite provider implementation
