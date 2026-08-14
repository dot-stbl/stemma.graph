# Hitl Interrupt Specification

## Purpose

Defines human-in-the-loop pause and resume: nodes signal interrupt via result status (not control-flow exceptions), checkpoints record interrupt state, and hosts resume with a command payload.

## Requirements

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

### Requirement: Closed resume command taxonomy
Resume commands MUST use a closed kind set for 0.2: `approve`, `reject`, and `update` (string constants / factories on `Command`, not free-form control-flow enums with behavior). The public API MUST expose factories or constants so hosts can document the contract without inventing kind strings.

#### Scenario: Approve factory
- **WHEN** the host resumes with `Command.Approve(payload?)`
- **THEN** the runtime treats kind as `approve`, injects payload as resume input to the interrupted node, and continues supersteps

#### Scenario: Reject factory
- **WHEN** the host resumes with `Command.Reject(reason?)`
- **THEN** the runtime treats kind as `reject`, injects reason as resume payload, and the interrupted node decides terminal vs re-route (runtime does not auto-fail solely because of reject)

#### Scenario: Update factory merges channels
- **WHEN** the host resumes with `Command.Update(values)` where values is a non-empty channel map
- **THEN** those channel writes are applied to checkpoint state before the interrupted node re-runs

### Requirement: Invalid command kind or payload
Resume MUST fail with a stable machine code when the command kind is missing, unknown, or violates kind-specific rules (e.g. `update` without values). The failure MUST NOT advance or clear the interrupted checkpoint incorrectly.

#### Scenario: Unknown kind
- **WHEN** resume is called with kind not in `{approve, reject, update}` (including empty/null)
- **THEN** the API fails with code `hitl.invalid_command` and the thread remains interrupted

#### Scenario: Update without values
- **WHEN** resume is called with kind `update` and empty or missing `Values`
- **THEN** the API fails with code `hitl.invalid_command`

### Requirement: Reject resume when not interrupted
Resume MUST fail clearly when the thread is not in interrupted status (or has no checkpoint).

#### Scenario: Resume on running/done thread
- **WHEN** resume is called for a thread whose status is done
- **THEN** the API returns a distinct error and does not mutate checkpoints incorrectly
