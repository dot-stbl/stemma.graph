# Graph Topology Specification

## Purpose

Defines how consumers declare graph topology (nodes, static edges, conditional edges) and what compile-time validation the builder MUST enforce before a graph becomes runnable.

## Requirements

### Requirement: Named nodes
The builder MUST allow registering nodes by unique string name. Duplicate node names at compile MUST fail with a clear error.

#### Scenario: Duplicate node name
- **WHEN** two nodes are registered with the same name
- **THEN** compile fails and reports the conflicting name

### Requirement: Static edges
The builder MUST support static edges from a source node (or START) to a target node (or END).

#### Scenario: Linear chain
- **WHEN** edges START→A, A→B, B→END are registered and the graph is compiled
- **THEN** a run that completes A then B reaches done after B without conditional routing

### Requirement: Conditional edges
The builder MUST support routing functions that, given current state (or a typed view), select the next node name(s) or END after a source node completes.

#### Scenario: Branch on status
- **WHEN** node A finishes and a conditional edge maps status `tools` → `tools` and otherwise → END
- **THEN** the next superstep ready set contains only the selected target

### Requirement: START entry required
Compile MUST fail if no edge originates from START (or equivalent entry registration), so every compiled graph has a defined entry.

#### Scenario: Missing START
- **WHEN** nodes exist but no START edge is registered
- **THEN** compile fails with an entry-missing error

### Requirement: Unknown edge endpoints rejected
Compile MUST fail if an edge references a node name that was not registered (except START/END sentinels).

#### Scenario: Edge to missing node
- **WHEN** an edge targets `tools` but no node named `tools` exists
- **THEN** compile fails naming the unknown endpoint

### Requirement: Cycles allowed
Compile MUST allow cycles (agent loops). Cycles are not a validation error; recursion limits govern runtime termination.

#### Scenario: ReAct cycle
- **WHEN** edges form agent→tools→agent
- **THEN** compile succeeds
