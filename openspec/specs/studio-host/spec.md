# Studio Host API Specification

## Purpose

Defines the stable **versioned HTTP + SSE** contract for ops Studio (and any FE)
against a single compiled graph + checkpointer. The contract is not coupled to
Razor UI internals (`MapVolutaUI`); hosts map it via `MapStudioApi` under
`/api/v1` (or a configured prefix). The bundled **Studio SPA** (React + Fluent,
embedded in `Voluta.UI` under `{prefix}/studio`) is the product surface
consuming this contract.

## Requirements

### Requirement: Versioned path prefix
Studio routes MUST live under a versioned path prefix (default `/api/v1`).
Bump of the major API shape requires a new prefix segment (e.g. `/api/v2`).

#### Scenario: Default prefix
- **WHEN** a host calls `MapStudioApi()` without options
- **THEN** routes are registered under `/api/v1/...`

### Requirement: Topology export
`GET {prefix}/topology` MUST return the wire projection of
`CompiledGraph.Describe()` (nodes, channels, static edges, conditional sources,
recursion limit).

#### Scenario: Topology after compile
- **WHEN** a client requests topology for a compiled multi-node graph
- **THEN** the response lists every node name and channel kind without invoking
  a run

### Requirement: Thread discovery and summaries
`GET {prefix}/threads` MUST list known thread ids (in-process track +
`IThreadDiscovery` when available) with status, step, last node, and optional
goal summary fields.

#### Scenario: Durable discovery after restart
- **WHEN** the checkpointer implements `IThreadDiscovery` and has put checkpoints
- **THEN** thread list includes those ids without re-tracking in the host process

### Requirement: Thread state and history
`GET {prefix}/threads/{id}` MUST return the host-facing `ThreadSnapshot` wire
shape (or 404 when missing). `GET {prefix}/threads/{id}/history` MUST return
ordered history steps or **501** with code `checkpoint.list_not_supported` when
the store cannot list.

#### Scenario: Missing thread
- **WHEN** a client requests state for an unknown thread id
- **THEN** the host responds with HTTP 404

#### Scenario: List not supported
- **WHEN** history is requested against a checkpointer that throws
  `NotSupportedException` on list
- **THEN** the host responds with HTTP 501 and a stable machine code

### Requirement: HITL resume
`POST {prefix}/threads/{id}/resume` MUST accept a body with optional `kind`
(default `approve`) and `payload`, map to the closed `Command` taxonomy
(approve / reject / update), and return a terminal stream event wire shape.

#### Scenario: Approve interrupted thread
- **WHEN** a thread is Interrupted and the client posts `{ "kind": "approve" }`
- **THEN** the host resumes the run and returns a terminal event (End or further
  Interrupt)

#### Scenario: Invalid kind
- **WHEN** the client posts an unknown kind
- **THEN** the host responds with HTTP 400 and code `studio.invalid_command`

### Requirement: Continue, update, and fork
Hosts MUST expose:
- `POST {prefix}/threads/{id}/continue` → `ContinueInvokeAsync`
- `POST {prefix}/threads/{id}/update` with channel `writes` → `UpdateStateAsync`
- `POST {prefix}/threads/{id}/fork` with `step` + `newThreadId` → `ForkAsync`

#### Scenario: Update requires writes
- **WHEN** update is posted without writes
- **THEN** the host responds with HTTP 400 and code `studio.invalid_request`

### Requirement: SSE stream
`GET {prefix}/threads/{id}/stream` MUST emit `text/event-stream` frames with
`event: stream` data as stream-event wire JSON and a final `event: done`.
Query `mode` selects checkpoint (default), resume, continue, or invoke.

#### Scenario: Checkpoint snapshot stream
- **WHEN** mode is omitted or `checkpoint` for an existing thread
- **THEN** the host emits at least one stream event derived from the latest
  checkpoint and then `done`

### Requirement: HITL queue
`GET {prefix}/hitl` MUST list interrupted threads with step, last node, and
interrupt payload string form.

#### Scenario: Only interrupted
- **WHEN** the store has Done and Interrupted threads
- **THEN** hitl list contains only Interrupted rows

### Requirement: Optional API key
Authentication MUST be optional and off by default. When `StudioApiOptions.ApiKey`
is non-empty, every Studio route MUST require `X-Api-Key` or
`Authorization: Bearer {key}` and respond **401** with code `studio.unauthorized`
when missing or wrong.

#### Scenario: Auth disabled
- **WHEN** ApiKey is null or empty
- **THEN** unauthenticated clients can call topology and threads

#### Scenario: Auth enabled
- **WHEN** ApiKey is configured and the client omits the key
- **THEN** the host responds with HTTP 401

### Requirement: Sample host
The repository MUST ship a sample (`samples/StudioHost`) that registers
`MapStudioApi`, seeds demo threads, and documents the endpoint table.

#### Scenario: Sample builds
- **WHEN** the solution is built
- **THEN** `samples/StudioHost` compiles as a non-packable Web project

### Requirement: Graph exception mapping
Mutation endpoints MUST map runtime `GraphException` subtypes onto stable HTTP
responses instead of unhandled 500s: `GraphThreadNotFoundException` /
`GraphStepNotFoundException` → **404**; `GraphInvalidResumeException` /
`GraphInvalidContinueException` / `GraphOutOfStepsException` → **409**;
`GraphInvalidCommandException` → **400**. The body carries `{ error, code }`
with the stable dot-case `GraphException.Code`.

#### Scenario: Resume a done thread
- **WHEN** a client posts resume to a thread whose status is Done
- **THEN** the host responds with HTTP 409 and code `graph.invalid_resume`

#### Scenario: Continue a done thread
- **WHEN** a client posts continue to a thread whose status is Done
- **THEN** the host responds with HTTP 409 and a `graph.*` code

### Requirement: Bundled Studio SPA
`Voluta.UI` MUST embed the Studio SPA build output (`wwwroot/studio`) and serve
it under `{MapVolutaUI prefix}/studio` with a single catch-all route: empty
asset path → index, extensionless paths → SPA fallback (client routing), known
extensions → embedded asset bytes or 404. When the SPA is not built, the host
MUST stay up and respond with a 503 HTML notice.

#### Scenario: SPA index and deep links
- **WHEN** the SPA is built and a client requests `{prefix}/studio` or a
  client-side route like `{prefix}/studio/threads/{id}`
- **THEN** the host responds with the SPA index HTML (base href injected)

#### Scenario: SPA not built
- **WHEN** `wwwroot/studio/index.html` is absent and a client requests
  `{prefix}/studio`
- **THEN** the host responds 503 with build instructions and stays healthy

#### Scenario: No ambiguous route matches
- **WHEN** routes for `{prefix}/studio`, `{prefix}/studio/`, and
  `{prefix}/studio/{**assetPath}` are registered
- **THEN** only the catch-all matches (no `AmbiguousMatchException`)
