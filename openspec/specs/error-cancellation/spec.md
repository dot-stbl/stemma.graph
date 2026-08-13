# Error Cancellation Specification

## Purpose

Defines how node failures, superstep failures, and cooperative cancellation affect run status, checkpoints, and streaming—so hosts can handle faults without undefined behavior.

## Requirements

### Requirement: Node exception fails the run
If a node task throws an exception that is not translated into a continue/interrupt result, the runtime MUST mark the run as failed, surface the error to the host, and MUST NOT apply that task’s incomplete writes as a successful merge.

#### Scenario: Tool node throws
- **WHEN** a ready tools node throws during a superstep
- **THEN** the run status becomes failed, stream/events include a failure signal, and the host receives the exception (or a wrapped graph fault)

### Requirement: Failed run checkpoint policy
On failure after a prior successful superstep, the last successfully committed checkpoint MUST remain loadable. The runtime MUST document whether a failure checkpoint is also written; if written, it MUST record failed status without corrupting channel values from the last good step.

#### Scenario: Resume after failure not automatic
- **WHEN** a run fails on superstep N+1 and the host loads the thread
- **THEN** the host can read the last good checkpoint and decide whether to retry; the runtime does not silently continue past the failure

### Requirement: Concurrent multi-write LastValue is a failure
A superstep that violates LastValue single-writer rules MUST fail the run (or that superstep) with a concurrent-update error, consistent with state-channels requirements.

#### Scenario: Two LastValue writers
- **WHEN** two tasks write the same LastValue channel in one superstep
- **THEN** status is failed (or equivalent fault) and the illegal merge is not committed as success

### Requirement: Cancellation is cooperative
When the invocation cancellation token is signaled, the runtime MUST stop scheduling further supersteps after the current barrier policy completes or is aborted, and MUST not leave the checkpointer in a torn undefined state.

#### Scenario: Cancel during long node
- **WHEN** the host cancels while a node is running and the node observes the token
- **THEN** the run ends in a cancelled/faulted terminal state and the last committed checkpoint remains consistent

### Requirement: Cancel does not equal interrupt
Cancellation MUST NOT be treated as HITL interrupt. Resume-after-interrupt APIs MUST reject a cancelled run the same way they reject non-interrupted threads, unless the product later defines an explicit rehydrate path.

#### Scenario: Resume after cancel
- **WHEN** a run was cancelled and the host calls ResumeAsync
- **THEN** resume fails with a distinct not-interrupted (or invalid state) error

### Requirement: Stream observes terminal faults
Streaming consumers MUST observe a terminal item or completion path that distinguishes done, interrupted, failed, and cancelled (via mode `events` and/or stream completion with error).

#### Scenario: Failed run on stream
- **WHEN** a node fails during StreamAsync
- **THEN** the stream surfaces failure (exception on enumerate and/or a failed event) and does not hang waiting for further supersteps
