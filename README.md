<p align="center">
  <a href="https://github.com/dot-stbl/stemma.graph">
    <img src="https://raw.githubusercontent.com/dot-stbl/stemma.graph/main/assets/banner.png"
         alt="StemmaGraph — stateful, cyclic, durable agent graphs for .NET">
  </a>
</p>

<p align="center">
  <a href="https://github.com/dot-stbl/stemma.graph/actions/workflows/ci.yml"><img alt="CI" src="https://github.com/dot-stbl/stemma.graph/actions/workflows/ci.yml/badge.svg" /></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" /></a>
  <a href="https://github.com/dot-stbl/stemma.graph/blob/main/LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" /></a>
  <a href="https://github.com/dot-stbl/stemma.graph/issues/1"><img alt="Status: pre-release" src="https://img.shields.io/badge/status-pre--release-orange?style=flat-square" /></a>
</p>

Agents that **loop until they're done** — and survive the process that ran them. StemmaGraph is a
low-level orchestration runtime for .NET: you describe a graph of nodes and edges, some of them
cyclic, and it executes the graph in Pregel-style supersteps with typed state, durable
checkpoints, streaming, and human-in-the-loop interrupts.

Our design bets:

```text
→ cycles not DAGs
→ typed channels not dictionary soup
→ checkpoints not in-process memory
→ .NET-native not a Python port
→ AOT-ready core not a kitchen sink
```

> [!IMPORTANT]
> **Pre-release.** The MVP runtime is on `main` — Pregel engine, in-memory checkpointer, source
> generator, testing package, five samples, BenchmarkDotNet baselines. **Nothing is on NuGet
> yet**; the 0.1 tag is the next milestone ([epic #1](https://github.com/dot-stbl/stemma.graph/issues/1)).
> Until then, reference the projects from source — see [Quick Start](#quick-start).

## See it in action

A ReAct agent that calls tools, loops back to think again, and stops on its own. This is the real
output of `dotnet run --project samples/01-HelloWorld` (middle rounds trimmed):

```text
StemmaGraph sample 01 — simulated ReAct (agent ⇄ tools)
Thread: react-sample-1

[agent] round 0: requesting tools
stream step=1 kind=Updates nodes=[agent]
  write status = tools
  write messages = agent: call get_weather (round 1)
[tools] executing simulated tool (round 1)
stream step=2 kind=Updates nodes=[tools]
  write tool_rounds = 1
  write messages = tools: observation — temp=12C (round 1)
  write status = agent
…
[agent] enough tool data — finishing
stream step=5 kind=Updates nodes=[agent]
  write status = done
  write messages = agent: final answer — cloudy, 12°C in Oslo
stream step=5 kind=End nodes=[-]

Final status: Done
Messages:
  - user: what's the weather in Oslo?
  - agent: call get_weather (round 1)
  - tools: observation — temp=12C (round 1)
  - agent: call get_weather (round 2)
  - tools: observation — temp=12C (round 2)
  - agent: final answer — cloudy, 12°C in Oslo
```

Nothing above is a framework convention you have to learn. `messages` accumulates because it was
declared `Append`; `status` replaces because it was declared `LastValue`; the loop exists because
one edge is conditional:

```csharp
var graph = new StateGraph()
    .AddChannel("messages", ChannelKind.Append)
    .AddChannel("status", ChannelKind.LastValue)
    .AddNode("agent", AgentNodeAsync)
    .AddNode("tools", ToolsNodeAsync)
    .AddEdge(GraphConstants.Start, "agent")
    .AddConditionalEdges(                                  // ← the cycle
        "agent",
        static context => context.Read<string>("status") == "tools" ? "tools" : GraphConstants.End)
    .AddEdge("tools", "agent")
    .Compile(checkpointer, new CompileOptions { RecursionLimit = 32 });
```

<details>
<summary><strong>Pausing for a human, then resuming — days later, in another process</strong></summary>

A node returns an interrupt instead of writes. The run stops, the checkpoint holds the payload, and
`ResumeAsync` picks it up with a decision. Real output of `samples/02-InterruptResume`:

```text
=== Invoke (expect interrupt) ===
stream step=0 kind=Start nodes=[-]
[gate] interrupting for human approval
stream step=1 kind=Interrupt nodes=[gate]
  payload = { action = transfer, amount = 50, currency = USD }

Checkpoint status after invoke: Interrupted
Interrupt payload: { action = transfer, amount = 50, currency = USD }

=== Resume with Command.Kind = approve ===
stream step=1 kind=Start nodes=[-]
[gate] resumed with payload=ok — approving
stream step=2 kind=End nodes=[-]

Checkpoint status after resume: Done
Messages:
  - user: transfer $50
  - gate: transfer approved
```

The node decides by looking at `context.ResumePayload` — no exceptions used for control flow:

```csharp
static Task<NodeResult> GateNodeAsync(GraphContext context, CancellationToken cancellationToken)
{
    if (context.ResumePayload is null)
    {
        return Task.FromResult<NodeResult>(
            NodeResult.Interrupt(new { action = "transfer", amount = 50, currency = "USD" }));
    }

    return Task.FromResult<NodeResult>(
        NodeResult.Continue(new ChannelWrite("messages", "gate: transfer approved")));
}
```

`ResumeAsync(threadId, command)` is a separate call against the same thread id, so the approval can
arrive from an HTTP handler long after the original run ended — or from a different process.

</details>

<details>
<summary><strong>Typed state instead of string keys</strong></summary>

String channel names work, but you don't have to live with them. Annotate a partial class and the
source generator emits the schema plus a partial update type:

```csharp
[GraphState]
public partial class ReviewState
{
    [Channel(ChannelKind.Append)]
    public IList<object?> Notes { get; set; } = new List<object?>();

    [Channel(ChannelKind.LastValue)]
    public string? Verdict { get; set; }
}
```

You get `ReviewState.CreateSchema()` and `ReviewState.ReviewStateUpdate`, where **unset properties
emit no write at all** — an explicit `null` is a clear, not "unchanged":

```csharp
var graph = new StateGraph()
    .AddChannels(ReviewState.CreateSchema())
    .AddNode(
        "review",
        static (context, cancellationToken) => Task.FromResult<NodeResult>(
            NodeResult.Continue(new ReviewState.ReviewStateUpdate { Verdict = "approved" }.ToWrites())))
    .AddEdge(GraphConstants.Start, "review")
    .AddEdge("review", GraphConstants.End)
    .Compile(new InMemoryCheckpointer());
```

Interface-typed properties need `OptionalValue<IList<object?>>.Of(value)` — C# forbids user-defined
conversions involving interfaces. The generator refuses to run on a non-partial class or one with
no `[Channel]` properties, and tells you which.

</details>

<details>
<summary><strong>Testing a graph without fighting it</strong></summary>

`StemmaGraph.Testing` ships the doubles you'd otherwise write by hand:

- `RecordingCheckpointer` — records every `Put` / `Get` / `List` on any inner checkpointer.
- `FaultInjectingCheckpointer` — fails the *n*-th write, so you can assert the run survives it.
- `CheckpointerConformance.RunAllAsync` — the suite every `ICheckpointer` must pass, interrupt
  fields and pending writes included. Bring your own storage and run it.
- `GraphFixtures.Linear()` / `.Cycle()` and `StreamCapture` — graphs and stream drains for tests.

</details>

## Why StemmaGraph

- **Cycles are the point** — an agent that reconsiders is a loop, not a pipeline. Conditional edges
  plus a `RecursionLimit` give you loops that terminate on purpose instead of by accident.
- **The run outlives the process** — every superstep boundary is a checkpoint. A thread can be
  interrupted, inspected, resumed, or replayed from storage; `ICheckpointer` is the only seam.
- **Multi-writer state is defined, not hoped for** — when two nodes in the same superstep write the
  same channel, the reducer decides the outcome. `Append` accumulates, `LastValue` replaces.
- **The core stays small** — runtime and abstractions are `IsAotCompatible` with zero third-party
  dependencies. No LLM SDK, no DI container, no logging framework in the hot path.

## Quick Start

**Requires the .NET 10 SDK (10.0.100 or newer).** Check with `dotnet --version`.

Nothing is published yet, so start from source:

```bash
git clone https://github.com/dot-stbl/stemma.graph.git
cd stemma.graph
dotnet build stemma.graph.slnx
```

Run a sample to see a live graph:

```bash
dotnet run --project samples/01-HelloWorld
```

Then point your own project at the runtime with
`dotnet add reference path/to/stemma.graph/src/StemmaGraph/StemmaGraph.csproj`.

Here is a complete graph — a writer and a critic that loop until the score clears the bar. It
prints `End after 4 supersteps`:

```csharp
using StemmaGraph;
using StemmaGraph.Abstractions.Channels;
using StemmaGraph.Abstractions.Results;
using StemmaGraph.Abstractions.Runtime;
using StemmaGraph.Checkpoint;
using StemmaGraph.Graph;
using StemmaGraph.Graph.Builder;
using StemmaGraph.Graph.Options;

var graph = new StateGraph()
    .AddChannel("draft", ChannelKind.LastValue)   // writes replace
    .AddChannel("notes", ChannelKind.Append)      // writes accumulate
    .AddChannel("score", ChannelKind.LastValue)
    .AddNode("write", WriteAsync)
    .AddNode("critique", CritiqueAsync)
    .AddEdge(GraphConstants.Start, "write")
    .AddEdge("write", "critique")
    .AddConditionalEdges(
        "critique",
        static context => (context.Read<int?>("score") ?? 0) < 8 ? "write" : GraphConstants.End)
    .Compile(new InMemoryCheckpointer(), new CompileOptions { RecursionLimit = 16 });

var final = await graph.InvokeAsync(
    [new ChannelWrite("draft", "checkpoints are nice")],
    new RunOptions { ThreadId = "post-42" });

Console.WriteLine($"{final.Kind} after {final.Step} supersteps");

static Task<NodeResult> WriteAsync(GraphContext context, CancellationToken cancellationToken)
{
    var round = (context.Read<int?>("score") ?? 0) / 4;
    return Task.FromResult<NodeResult>(
        NodeResult.Continue(
            new ChannelWrite("draft", $"revision {round + 1}"),
            new ChannelWrite("notes", $"writer: produced revision {round + 1}")));
}

static Task<NodeResult> CritiqueAsync(GraphContext context, CancellationToken cancellationToken)
{
    var score = (context.Read<int?>("score") ?? 0) + 4;
    return Task.FromResult<NodeResult>(
        NodeResult.Continue(
            new ChannelWrite("score", score),
            new ChannelWrite("notes", $"critic: scored {score}/10")));
}
```

Swap `InvokeAsync` for `StreamAsync` to observe the run as it happens
(`StreamMode.Values` / `Updates` / `Events`), and pass a real `ICheckpointer` when you want the
thread to outlive the process. Under a host, `services.AddStemmaGraph(provider => …)` compiles the
graph once and registers it as a singleton.

## How a superstep works

One tick of the engine, in order: collect every node made ready by the previous tick, run them
concurrently, **barrier**, merge their writes through the channel reducers, persist a checkpoint,
then evaluate edges to decide who runs next. Two consequences worth internalizing:

- Nodes in the same superstep never see each other's writes — they see the state as of the barrier.
  That is what makes concurrent nodes deterministic to reason about.
- A conditional edge is evaluated *after* the merge, on committed state, so routing decisions can't
  race with the writes they depend on.

Design notes and decision records live in
[`openspec/changes/architecture-runtime-core/`](https://github.com/dot-stbl/stemma.graph/tree/main/openspec/changes/architecture-runtime-core).

## How it compares

**vs. [LangGraph](https://github.com/langchain-ai/langgraph)** (Python, MIT) — The origin of this
execution model and still the richest ecosystem around it. StemmaGraph borrows the ideas
(supersteps, channels, checkpoint-first persistence) and rebuilds the surface on .NET generics,
typed reducers, and `IAsyncEnumerable` — no `TypedDict` reflection. Not a port; a peer.

**vs. Microsoft Agent Framework** — MAF is the better answer for multi-agent conversations and
function calling, and it has Microsoft behind it. It doesn't give you cyclic graphs with durable
per-thread state. These compose: run MAF agents *inside* StemmaGraph nodes.

**vs. Durable Functions / Durable Task** — Battle-tested durability with far more storage
providers, and the right tool for business workflows. Its programming model is orchestrator code
with replay semantics; StemmaGraph's is a graph with explicit state channels, which fits an
agent's think-act-observe loop more directly and keeps the loop bound visible.

**vs. rolling your own `while` loop** — Works until you need to answer "what was the state at
step 7?", "how do I resume after the pod restarted?", or "two nodes wrote the same field, now
what?". Those three questions are the entire library.

## Packages

| Package | Role | Status |
|---|---|---|
| `StemmaGraph.Abstractions` | Contracts: channels, checkpoints, `NodeResult`, streaming | on `main` |
| `StemmaGraph` | Pregel runtime + in-memory checkpointer | on `main` |
| `StemmaGraph.DependencyInjection` | `AddStemmaGraph` for `IServiceCollection` | on `main` |
| `StemmaGraph.Generators` | `[GraphState]` source generator | on `main` |
| `StemmaGraph.Testing` | Test doubles + checkpointer conformance suite | on `main` |
| `StemmaGraph.Checkpoints.*` | EF Core / S3 / file-system providers | planned |
| `StemmaGraph.MicrosoftAi` | `IChatClient` glue for `Microsoft.Extensions.AI` | planned |

**Native AOT** applies to the core tier only — `StemmaGraph`, `Abstractions`, and
`DependencyInjection` are `IsAotCompatible`, with a publish smoke test in `samples/03-AotSmoke`.
Checkpoint providers, UI, and AI integration will be regular-CLR packages; they will not claim AOT.

## Samples

| Sample | What it shows |
|---|---|
| [`01-HelloWorld`](https://github.com/dot-stbl/stemma.graph/tree/main/samples/01-HelloWorld) | Simulated ReAct loop (agent ⇄ tools), streaming updates |
| [`02-InterruptResume`](https://github.com/dot-stbl/stemma.graph/tree/main/samples/02-InterruptResume) | HITL interrupt and `Command` resume |
| [`03-AotSmoke`](https://github.com/dot-stbl/stemma.graph/tree/main/samples/03-AotSmoke) | Native AOT publish smoke test |
| [`04-ReviewBot`](https://github.com/dot-stbl/stemma.graph/tree/main/samples/04-ReviewBot) | CLI review harness: plan → sandboxed tools → review |
| [`05-DocQ`](https://github.com/dot-stbl/stemma.graph/tree/main/samples/05-DocQ) | Docs Q&A over a sandboxed folder |

## What isn't here yet

Stated plainly so you can judge the fit:

- **No published packages.** Source references only until the 0.1 tag.
- **No `Send` fan-out or subgraphs.** `Send` exists as a contract; the engine does not schedule it.
- **No durable checkpoint provider.** `InMemoryCheckpointer` only — a restart loses the thread.
  `ICheckpointer` is small on purpose if you want to implement one now.
- **No LLM integration in the box.** Nodes are your code; wiring `IChatClient` is on you until
  `StemmaGraph.MicrosoftAi` lands.
- **No UI.** A run inspector and HITL queue are on the roadmap, not in the repo.

## Development

```bash
dotnet build stemma.graph.slnx                      # 0 warnings, 0 errors — the gate
dotnet test stemma.graph.slnx                       # xUnit + Shouldly + NSubstitute
dotnet format stemma.graph.slnx --severity hidden   # style drift check
dotnet run -c Release --project benchmarks/StemmaGraph.Benchmarks
```

`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on for every project, so a clean build
*is* the style review. Benchmarks (`LinearInvoke`, `CycleFiveTicks`, `ParallelAppend`,
`CheckpointPutGet`) are not gated on PR CI — see
[#10](https://github.com/dot-stbl/stemma.graph/issues/10).

## Contributing

Small fixes go straight to a PR. For anything non-trivial, open an issue first so we can agree on
the shape before you write it — the runtime's contracts are still moving.

```bash
git config core.hooksPath .githooks   # once per clone
```

The `commit-msg` hook strips AI attribution trailers, so don't hand-add them; commits follow
`[stemma](feat/scope): subject`. AI-generated code is welcome when it's tested and you understand
it. Full setup, conventions, and the PR checklist:
[CONTRIBUTING.md](https://github.com/dot-stbl/stemma.graph/blob/main/CONTRIBUTING.md).

## Inspiration

The execution model comes from [LangGraph](https://github.com/langchain-ai/langgraph) (MIT) —
Pregel-style supersteps, channel/reducer state, checkpoint-first persistence. The API diverges
substantially, and any place where StemmaGraph is wrong about this design space is our own fault,
not theirs.

## License

MIT — see [LICENSE](https://github.com/dot-stbl/stemma.graph/blob/main/LICENSE).
