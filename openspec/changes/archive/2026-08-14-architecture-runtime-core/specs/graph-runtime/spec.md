## Purpose

Defines Pregel-style superstep execution for StemmaGraph: ready-set selection, parallel node execution with barrier, write application, and termination rules for stateful agent graphs.

## ADDED Requirements

### Requirement: Superstep advances only after barrier
The runtime SHALL execute a superstep by selecting all ready tasks, running them to completion (or failure), then applying their writes before any subsequent superstep can observe those writes.

#### Scenario: Writes from step N are invisible during step N
- **WHEN** two nodes are ready in the same superstep and both read shared state
- **THEN** each node observes the pre-superstep channel values, not the other node's writes from the same superstep

#### Scenario: Empty ready set ends the run
- **WHEN** `prepare_next_tasks` yields no ready tasks
- **THEN** the run status becomes completed (done) and no further supersteps execute

### Requirement: All ready tasks run in one superstep
The runtime MUST schedule every ready task for the current superstep (PULL-triggered nodes and PUSH/Send tasks) rather than executing only a single node per superstep.

#### Scenario: Multiple ready nodes
- **WHEN** the graph has two nodes whose triggers are satisfied after the previous superstep
- **THEN** both nodes execute within the same superstep before writes are applied

### Requirement: Deterministic write application order
Within a superstep, the runtime SHALL apply channel writes in a deterministic order so multi-writer reducers produce stable results across runs with the same inputs.

#### Scenario: Two writers to an append channel
- **WHEN** two tasks write to the same append-reduced channel in one superstep
- **THEN** both values are merged according to the channel reducer and the merge order is stable for identical task identity ordering

### Requirement: Recursion limit
The runtime MUST stop a run that exceeds a configured maximum superstep count and surface a distinct out-of-steps failure (not a silent hang).

#### Scenario: Infinite cycle
- **WHEN** a cycle never reaches END and the superstep count exceeds the configured limit
- **THEN** the run fails with an out-of-steps error and the last successful checkpoint remains loadable

### Requirement: START and END sentinels
The graph model SHALL support a START entry edge and an END terminal so every compiled graph has a well-defined entry and at least one terminal path.

#### Scenario: First superstep from START
- **WHEN** a run is invoked with initial input
- **THEN** the first ready tasks are those reachable from START after input channels are seeded
