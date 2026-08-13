# Send Subgraphs Specification

## Purpose

Defines Send-style dynamic fan-out and subgraph composition as first-class architecture contracts.

**Status (main):** Send execution and a subgraph-as-node helper are **implemented**. Checkpoint field `PendingSends` preserves scheduled PUSH tasks. Richer nested lifecycle (child interrupt bubbling, channel rename maps) may still grow.

## Requirements

### Requirement: Send fan-out tasks
The model MUST support dynamic tasks that enqueue N invocations of a target node with distinct payloads (map-style fan-out), executed as PUSH tasks in a subsequent superstep. Nodes emit Sends via `NodeResult.ContinueWithSends` (or continue with writes + sends). Task payload is exposed as `GraphContext.TaskPayload`.

#### Scenario: Map over subjects
- **WHEN** a node emits Send to worker node for each item in a list
- **THEN** the next superstep schedules one task per item and merges worker writes via channel reducers

### Requirement: Pending Sends on checkpoint
Checkpoints MUST carry `PendingSends` (node name, payload, task id) so scheduled PUSH work is part of the C-shape snapshot. Empty list is valid for runs that never used Send.

#### Scenario: Checkpoint forward compatibility
- **WHEN** a checkpoint without pending Send tasks is loaded by a version that supports Send
- **THEN** the checkpoint still loads and runs without migration failure

### Requirement: Subgraph as compiled unit
A parent graph MUST be able to treat another compiled graph as a node (subgraph) with defined input/output channel mapping. Library helper: `Subgraph.AsNode(child, inputChannels, outputChannels)`.

#### Scenario: Nested agent
- **WHEN** a parent node is bound to a compiled child graph
- **THEN** invoking the parent runs the child to a terminal status and maps child outputs into parent channels per configuration

### Requirement: Topology export for tooling
Compiled graphs MUST expose a read-only topology description (nodes, static edges, conditional sources, channel kinds) for UI/tooling without leaking handlers.

#### Scenario: UI topology screen
- **WHEN** a host calls `CompiledGraph.Describe()`
- **THEN** it receives nodes, edges, and channel schema suitable for a read-only diagram
