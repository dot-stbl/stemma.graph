## Purpose

Defines Send-style dynamic fan-out and subgraph composition as first-class architecture contracts. MVP 0.1 MAY defer implementation but MUST NOT design the core runtime in a way that precludes these features.

## ADDED Requirements

### Requirement: Send fan-out tasks
The model MUST support dynamic tasks that enqueue N invocations of a target node with distinct payloads (map-style fan-out), executed as PUSH tasks in a subsequent superstep.

#### Scenario: Map over subjects
- **WHEN** a node emits Send to worker node for each item in a list
- **THEN** the next superstep schedules one task per item and merges worker writes via channel reducers

### Requirement: Fan-out out of MVP ship is explicit
A released MVP MAY omit Send execution, but public architecture docs MUST mark Send as planned and MUST reserve task/PUSH slots in the checkpoint/runtime model so adding Send is non-breaking to C-shape checkpoints.

#### Scenario: Checkpoint forward compatibility
- **WHEN** an MVP checkpoint without pending Send tasks is loaded by a later version that supports Send
- **THEN** the checkpoint still loads and runs without migration failure

### Requirement: Subgraph as compiled unit
A parent graph MUST be able to treat another compiled graph as a node (subgraph) with defined input/output channel mapping.

#### Scenario: Nested agent
- **WHEN** a parent node is bound to a compiled child graph
- **THEN** invoking the parent runs the child to a terminal status and maps child outputs into parent channels per configuration

### Requirement: Subgraph not required for MVP ship
MVP 0.1 MAY ship without subgraph APIs. Core runtime types for tasks and checkpoints MUST include documented extension points so parent/child channel boundaries remain possible without a breaking redesign.

#### Scenario: Design review gate
- **WHEN** core runtime types for tasks and checkpoints are reviewed
- **THEN** they include extension points documented for subgraph invocation and Send payloads
