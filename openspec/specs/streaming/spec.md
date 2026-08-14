# Streaming Specification

## Purpose

Defines multi-mode streaming as the primary observation API for graph runs, with invoke-to-completion as a convenience over the stream.

## Requirements

### Requirement: Multi-mode stream
The runtime MUST support streaming modes at least: `values` (state snapshots), `updates` (per-superstep or per-node deltas), `events` (lifecycle/control events such as start, interrupt, end), and `messages` (lifecycle plus node-emitted custom/token items).

#### Scenario: Subscribe to updates
- **WHEN** a host streams with mode `updates`
- **THEN** it receives ordered items describing which nodes/tasks produced which channel writes without requiring a full state dump each time

#### Scenario: Subscribe to values
- **WHEN** a host streams with mode `values`
- **THEN** it receives successive full (or projected) state snapshots after supersteps commit

#### Scenario: Subscribe to messages
- **WHEN** a host streams with mode `messages`
- **THEN** it receives Start/End lifecycle events and any Custom/Messages items written by nodes, without Values or Updates dumps

### Requirement: Node stream writer
Nodes MUST be able to emit custom progress payloads and LLM token fragments while executing, via `GraphContext.Stream` (`IStreamWriter`). The runtime MUST inject a writer that forwards items into the live `IAsyncEnumerable<StreamEvent>` for all stream modes (values, updates, events, messages).

#### Scenario: Custom progress during node body
- **WHEN** a node calls `context.Stream.WriteCustomAsync(payload)` mid-execution
- **THEN** the host receives a `StreamEvent` with `Kind = Custom` and the payload before (or as part of) that superstep’s commit events

#### Scenario: Token fragments during node body
- **WHEN** a node calls `context.Stream.WriteMessageAsync(text)` mid-execution
- **THEN** the host receives a `StreamEvent` with `Kind = Messages` and the text as `Payload`

### Requirement: MEAI token bridge
`Voluta.Agents.AI` chat nodes MUST optionally bridge `IChatClient.GetStreamingResponseAsync` deltas into the graph stream as `Messages` events, while still writing the full assistant text to the configured output channel at node completion.

#### Scenario: Streaming chat client
- **WHEN** `ChatClientGraphNode` is configured with `Stream = true` and the host runs `StreamAsync`
- **THEN** each non-empty text delta appears as a `Messages` stream item and the channel receives the concatenated text

### Requirement: Async enumerable surface
Streaming MUST be exposed as an asynchronous sequence consumable with cancellation, suitable for ASP.NET SSE/gRPC bridging. Existing UI SSE writers that serialize `StreamEvent` already surface Custom/Messages kinds without a separate protocol.

#### Scenario: Cancellation mid-run
- **WHEN** the consumer cancels the stream token mid-run
- **THEN** the runtime stops scheduling further supersteps and honors cancellation without corrupting the last committed checkpoint

### Requirement: Invoke composes stream
A non-streaming invoke API MUST complete a run to terminal status (done, interrupted, or failed) and return the final result, equivalent to draining the stream to completion under documented default mode.

#### Scenario: Invoke until interrupt
- **WHEN** invoke runs a graph that hits an interrupt
- **THEN** invoke returns interrupted status with payload rather than hanging for resume

### Requirement: Bounded live stream backpressure
Live Custom/Messages delivery uses a bounded buffer. When full, further node writes MAY be dropped and MUST increment metric `voluta.stream.dropped` (tag `stream.kind`). Order is best-effort within a node; global order across parallel nodes is not guaranteed.

#### Scenario: Flood drops with metric
- **WHEN** a node emits more live custom events than the buffer capacity during a superstep
- **THEN** the run still reaches a terminal event and dropped count increases