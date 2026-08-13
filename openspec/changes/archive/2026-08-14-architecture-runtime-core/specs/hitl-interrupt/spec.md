## Purpose

Defines human-in-the-loop pause and resume: nodes signal interrupt via result status (not control-flow exceptions), checkpoints record interrupt state, and hosts resume with a command payload.

## ADDED Requirements

### Requirement: Interrupt via node result
A node MUST be able to pause the run by returning an interrupt result that includes a serializable payload. The runtime MUST NOT require throwing an exception to express HITL pause.

#### Scenario: Approval gate
- **WHEN** a payment node returns interrupt with payload `{ "amount": 50 }`
- **THEN** the run status becomes interrupted, a checkpoint is stored with that payload, and no further supersteps run until resume

### Requirement: Continue via node result
A node that completes normally MUST return a continue result carrying its partial channel writes (or empty writes).

#### Scenario: Normal tool node
- **WHEN** a tools node finishes successfully with message writes
- **THEN** the runtime applies those writes and schedules the next superstep without entering interrupted status

### Requirement: Resume with command
The public API MUST allow resuming an interrupted thread with a command payload that the runtime injects according to documented rules (e.g. as channel writes or as resume input to the interrupted node).

#### Scenario: User approves
- **WHEN** a thread is interrupted and the host calls resume with an approve command
- **THEN** the run leaves interrupted status and continues supersteps from the interrupted point using the command as specified

### Requirement: Reject resume when not interrupted
Resume MUST fail clearly when the thread is not in interrupted status (or has no checkpoint).

#### Scenario: Resume on running/done thread
- **WHEN** resume is called for a thread whose status is done
- **THEN** the API returns a distinct error and does not mutate checkpoints incorrectly
