# Source Gen State Specification

## Purpose

Defines optional source generation that maps a typed state declaration to channels, partial updates, and write emission—without making generation mandatory for all graphs.

## Requirements

### Requirement: Typed state declaration
A consumer MUST be able to declare graph state as a typed model annotated for channel/reducer metadata and obtain generated helpers for schema and partial updates.

#### Scenario: Append messages property
- **WHEN** a state model marks a list property with an append reducer
- **THEN** the generated schema registers that property’s channel as append-reduced

### Requirement: Typed partial update type
Generation MUST produce a partial update type where omitted properties mean “no write” and present properties mean “write this value” (including explicit null where supported).

#### Scenario: Update only status
- **WHEN** a node returns a partial update with only `Status` set
- **THEN** generated `ToWrites` emits a write solely for the status channel

### Requirement: Optional generation
Graphs without generated state MUST remain fully supported via fluent channel APIs. Generation MUST NOT be required to compile or run the runtime.

#### Scenario: No generator in project
- **WHEN** a project references runtime packages but not the generator
- **THEN** fluent graphs still compile and execute

### Requirement: Compile-time channel name safety for generated graphs
For generated state types, node APIs MUST bind updates so renames of state properties break at compile time rather than at runtime string mismatch.

#### Scenario: Rename property
- **WHEN** a generated property `Messages` is renamed and call sites are updated by the IDE
- **THEN** no residual string `"messages"` in node update code is required for that graph’s typed path
