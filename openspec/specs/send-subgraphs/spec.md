# Send Subgraphs Specification

## Purpose

Defines Send-style dynamic fan-out and subgraph composition as first-class architecture contracts.

**Status (main):** Send execution and subgraph-as-node with nested HITL/checkpoint lifecycle are **implemented**. Checkpoint field `PendingSends` preserves scheduled PUSH tasks. Channel rename maps remain optional growth.

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

### Requirement: Parallel Send interrupts resume by task id
When multiple Send-scheduled tasks interrupt in the same superstep, each pending interrupt MUST retain the Send task id and task payload so hosts can approve/reject per task via `Command.Resumes`. Single-interrupt Send paths keep working with `Command.Payload` alone.

#### Scenario: Map fan-out with dual approval gates
- **WHEN** two Send workers interrupt with distinct task ids
- **THEN** the host resumes with a map id → payload and both workers continue; channel writes merge via reducers after the barrier

### Requirement: Subgraph as compiled unit
A parent graph MUST be able to treat another compiled graph as a node (subgraph) with defined input/output channel mapping. Library helper: `Subgraph.AsNode(child, inputChannels, outputChannels, threadIdFactory?)`.

#### Scenario: Nested agent
- **WHEN** a parent node is bound to a compiled child graph
- **THEN** invoking the parent runs the child to a terminal status and maps child outputs into parent channels per configuration

### Requirement: Nested checkpoint namespace
A subgraph node MUST checkpoint the child under a dedicated thread id so parent and child C-shapes do not collide. Default factory is `{parentThreadId}/{nodeName}` using `GraphContext.ThreadId` and `GraphContext.NodeName`. Hosts MAY supply `threadIdFactory` for multi-agent nest namespaces.

#### Scenario: Default nested thread id
- **WHEN** a parent run with thread id `parent-1` enters subgraph node `nested`
- **THEN** the child run uses thread id `parent-1/nested` (unless a custom factory overrides)

#### Scenario: Custom thread id factory
- **WHEN** `Subgraph.AsNode` is given a custom `threadIdFactory`
- **THEN** the child checkpointer keys use the factory result on both first invoke and parent resume

### Requirement: Child interrupt bubbles to parent
When a child graph reaches `Interrupted`, the subgraph node MUST return `NodeResult.Interrupt` with the child interrupt payload so the parent checkpoint records the subgraph node in `NextNodes` / `LastNode` and surfaces a parent-level interrupt to the host.

#### Scenario: Child HITL surfaces on parent
- **WHEN** a node inside the child returns `NodeResult.Interrupt`
- **THEN** the parent stream ends with `StreamEventKind.Interrupt`, parent `NextNodes` is the subgraph node name, and the child thread remains `Interrupted` under its nested thread id

### Requirement: Parent resume continues nested child
When the host resumes the parent thread after a subgraph interrupt, the subgraph node MUST resume the **same** child thread (not re-invoke from START) with a `Command` derived from the parent resume payload (`Command` passthrough, else `Command.Approve(payload)`).

#### Scenario: Child interrupt → parent resume → child complete
- **WHEN** the host calls `ResumeInvokeAsync` on the parent after a child interrupt
- **THEN** the child gate receives resume payload, child reaches `Done`, parent maps `outputChannels` and continues to subsequent parent nodes

### Requirement: Topology export for tooling
Compiled graphs MUST expose a read-only topology description (nodes, static edges, conditional sources, channel kinds) for UI/tooling without leaking handlers.

#### Scenario: UI topology screen
- **WHEN** a host calls `CompiledGraph.Describe()`
- **THEN** it receives nodes, edges, and channel schema suitable for a read-only diagram
