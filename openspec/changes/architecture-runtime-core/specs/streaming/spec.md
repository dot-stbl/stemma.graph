## Purpose

Defines multi-mode streaming as the primary observation API for graph runs, with invoke-to-completion as a convenience over the stream.

## ADDED Requirements

### Requirement: Multi-mode stream
The runtime MUST support streaming modes at least: `values` (state snapshots), `updates` (per-superstep or per-node deltas), and `events` (lifecycle/control events such as start, interrupt, end).

#### Scenario: Subscribe to updates
- **WHEN** a host streams with mode `updates`
- **THEN** it receives ordered items describing which nodes/tasks produced which channel writes without requiring a full state dump each time

#### Scenario: Subscribe to values
- **WHEN** a host streams with mode `values`
- **THEN** it receives successive full (or projected) state snapshots after supersteps commit

### Requirement: Async enumerable surface
Streaming MUST be exposed as an asynchronous sequence consumable with cancellation, suitable for ASP.NET SSE/gRPC bridging.

#### Scenario: Cancellation mid-run
- **WHEN** the consumer cancels the stream token mid-run
- **THEN** the runtime stops scheduling further supersteps and honors cancellation without corrupting the last committed checkpoint

### Requirement: Invoke composes stream
A non-streaming invoke API MUST complete a run to terminal status (done, interrupted, or failed) and return the final result, equivalent to draining the stream to completion under documented default mode.

#### Scenario: Invoke until interrupt
- **WHEN** invoke runs a graph that hits an interrupt
- **THEN** invoke returns interrupted status with payload rather than hanging for resume
