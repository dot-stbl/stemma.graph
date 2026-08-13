# State Channels Specification

## Purpose

Defines named channels as the unit of graph state, reducer rules for multi-writer merge, partial updates from nodes, and how state schemas compile into channel maps.

## Requirements

### Requirement: State is a map of named channels
Graph state MUST be represented as named channels, each with a merge rule. Field-level typed views MAY project channel values but MUST NOT bypass channel update semantics.

#### Scenario: Read after merge
- **WHEN** a channel has been updated via its reducer
- **THEN** subsequent reads of that channel name return the merged value

### Requirement: LastValue rejects concurrent multi-write
A LastValue channel SHALL accept at most one write per superstep. If more than one write targets the same LastValue channel in one superstep, the runtime MUST fail that superstep with a concurrent-update error.

#### Scenario: Two LastValue writes in one superstep
- **WHEN** two ready tasks both write to channel `status` configured as LastValue
- **THEN** apply_writes fails with a concurrent-update error and the superstep does not commit a partial LastValue

### Requirement: Append (binop) reducer allows multi-write
An append (binary-operator aggregate) channel MUST accept multiple writes per superstep and combine them with the registered reducer function.

#### Scenario: Parallel tool messages
- **WHEN** two tasks write message lists to an append channel `messages` in one superstep
- **THEN** the channel value after apply contains the combined messages from both writes

### Requirement: Nodes emit partial updates only
A node MUST return partial channel writes (or a typed partial update that compiles to channel writes). A node MUST NOT be required to return a full state snapshot.

#### Scenario: Untouched channels preserved
- **WHEN** a node writes only to `messages` and does not mention `status`
- **THEN** after apply, `status` retains its previous channel value

### Requirement: Omitted vs explicit clear
The update model MUST distinguish “channel not updated” from “channel set to null/empty” so reducers and LastValue can implement clear semantics without ambiguity.

#### Scenario: Explicit null on LastValue
- **WHEN** a partial update sets a LastValue channel to null using an explicit present marker
- **THEN** the channel value becomes null (or empty per channel rules), not “unchanged”

### Requirement: Escape hatch without source generation
Consumers MUST be able to register channels and emit writes without source-generated types (fluent/string channel APIs).

#### Scenario: Dynamic channel registration
- **WHEN** a graph is built with fluent channel registration only (no generated state type)
- **THEN** compile and run succeed for nodes that write those channel names
