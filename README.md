<p align="center">
  <a href="https://github.com/dot-stbl/voluta">
    <img src="https://raw.githubusercontent.com/dot-stbl/voluta/main/assets/banner.png"
         alt="Voluta — stateful, cyclic, durable agent graphs for .NET">
  </a>
</p>

<p align="center">
  <a href="https://github.com/dot-stbl/voluta/actions/workflows/ci.yml"><img alt="CI" src="https://github.com/dot-stbl/voluta/actions/workflows/ci.yml/badge.svg" /></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img alt=".NET 10" src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet&logoColor=white" /></a>
  <a href="https://github.com/dot-stbl/voluta/blob/main/LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/License-MIT-blue.svg?style=flat-square" /></a>
  <a href="https://github.com/dot-stbl/voluta/issues/1"><img alt="Status: pre-release" src="https://img.shields.io/badge/status-pre--release-orange?style=flat-square" /></a>
</p>

Agents that **loop until they're done** — and survive the process that ran them. Voluta is a
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
> **Pre-release.** On `main`: Pregel engine, checkpointers (InMemory · File · EF Core · S3),
> Send / subgraph helpers, source generator, Testing, MicrosoftAi helpers, `MapVolutaUI`,
> samples, BenchmarkDotNet. **Nothing is on NuGet yet**; the 0.1 tag is the next milestone
> ([epic #1](https://github.com/dot-stbl/voluta/issues/1)). Until then, reference projects
> from source — see [Quick Start](#quick-start).

## See it in action

A ReAct agent that calls tools, loops back to think again, and stops on its own. This is the real
output of `dotnet run --project samples/01-HelloWorld` (middle rounds trimmed):

```text
Voluta sample 01 — simulated ReAct (agent ⇄ tools)
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

`Voluta.Testing` ships the doubles you'd otherwise write by hand:

- `RecordingCheckpointer` — records every `Put` / `Get` / `List` on any inner checkpointer.
- `FaultInjectingCheckpointer` — fails the *n*-th write, so you can assert the run survives it.
- `CheckpointerConformance.RunAllAsync` — the suite every `ICheckpointer` must pass, interrupt
  fields and pending writes included. Bring your own storage and run it.
- `GraphFixtures.Linear()` / `.Cycle()` and `StreamCapture` — graphs and stream drains for tests.

</details>

## Why Voluta

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
git clone https://github.com/dot-stbl/voluta.git
cd voluta
dotnet build voluta.slnx
```

Run a sample to see a live graph:

```bash
dotnet run --project samples/01-HelloWorld
```

Then point your own project at the runtime with
`dotnet add reference path/to/voluta/src/Voluta/Voluta.csproj`.

Here is a complete graph — a writer and a critic that loop until the score clears the bar. It
prints `End after 4 supersteps`:

```csharp
using Voluta;
using Voluta.Abstractions.Channels;
using Voluta.Abstractions.Results;
using Voluta.Abstractions.Runtime;
using Voluta.Checkpoint;
using Voluta.Graph;
using Voluta.Graph.Builder;
using Voluta.Graph.Options;

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
thread to outlive the process. Under a host, `services.AddVoluta(provider => …)` compiles the
graph once and registers it as a singleton.

## Durable checkpoints

`ICheckpointer` is the only storage seam. Pick a provider at host startup with a fluent builder —
exactly one `Use*`:

```csharp
// In-process (tests / samples)
services.AddVolutaCheckpoints(c => c.UseInMemory());

// JSON files under a directory
services.AddVolutaCheckpoints(c => c.UseFile("./.voluta/checkpoints"));

// Your app DbContext (any EF provider: Npgsql, SqlServer, SQLite, …)
services.AddDbContextFactory<AppDbContext>(o => o.UseNpgsql(connectionString));
services.AddVolutaCheckpoints(c => c.UseEntityFrameworkCore<AppDbContext>());

// S3 / MinIO / R2 (register IAmazonS3 yourself)
services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(RegionEndpoint.EUCentral1));
services.AddVolutaCheckpoints(c => c.UseS3(o =>
{
    o.BucketName = "voluta";
    o.KeyPrefix = "runs";
}));
```

Wire the graph with the registered store:

```csharp
services.AddVoluta(sp =>
{
    var checkpointer = sp.GetRequiredService<ICheckpointer>();
    return new StateGraph()
        // …nodes, edges, channels…
        .Compile(checkpointer);
});
```

<details>
<summary><strong>Host DbContext shape (EF)</strong></summary>

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IVolutaCheckpointDbContext
{
    public DbSet<CheckpointRecord> Checkpoints => Set<CheckpointRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVolutaCheckpointModel(); // table voluta_checkpoints
        // …your entities…
    }
}
```

Schema ownership is yours: migrations or `EnsureCreated()` in tests. Column names follow
**your** EF naming convention (no forced snake_case). Snapshot payload is a JSON column via
EF value conversion — the checkpointer itself does not call `JsonSerializer`.

</details>

<details>
<summary><strong>Errors and limits</strong></summary>

- **Miss on Get** → `null` (not an exception).
- **Storage failures** → `CheckpointStoreException` with stable `Code`
  (`checkpoint.put_failed` / `get_failed` / `list_failed`). Graph logic still uses
  `GraphException` and friends.
- **Channel values** should be JSON-friendly (strings, numbers, lists of primitives). Complex
  CLR graphs may not round-trip — same wire limits for File, EF, and S3.
- **S3 keys:** `{prefix}/{safeThreadId}/{step:D12}.json`.

Every provider is exercised by `CheckpointerConformance.RunAllAsync` in `Voluta.Testing`
(InMemory, File, EF SQLite + EF InMemory, S3 with a fake client).

</details>

## How a superstep works

One tick of the engine, in order: collect every node made ready by the previous tick, run them
concurrently, **barrier**, merge their writes through the channel reducers, persist a checkpoint,
then evaluate edges to decide who runs next. Two consequences worth internalizing:

- Nodes in the same superstep never see each other's writes — they see the state as of the barrier.
  That is what makes concurrent nodes deterministic to reason about.
- A conditional edge is evaluated *after* the merge, on committed state, so routing decisions can't
  race with the writes they depend on.

Behavior contracts live in
[`openspec/specs/`](https://github.com/dot-stbl/voluta/tree/main/openspec/specs)
(12 capabilities). Planning history:
[`openspec/changes/archive/2026-08-14-architecture-runtime-core/`](https://github.com/dot-stbl/voluta/tree/main/openspec/changes/archive/2026-08-14-architecture-runtime-core).

## How it compares

**vs. [LangGraph](https://github.com/langchain-ai/langgraph)** (Python, MIT) — The origin of this
execution model and still the richest ecosystem around it. Voluta borrows the ideas
(supersteps, channels, checkpoint-first persistence) and rebuilds the surface on .NET generics,
typed reducers, and `IAsyncEnumerable` — no `TypedDict` reflection. Not a port; a peer.

**vs. Microsoft Agent Framework** — MAF is the better answer for multi-agent conversations and
function calling, and it has Microsoft behind it. It doesn't give you cyclic graphs with durable
per-thread state. These compose: run MAF agents *inside* Voluta nodes.

**vs. Durable Functions / Durable Task** — Battle-tested durability with far more storage
providers, and the right tool for business workflows. Its programming model is orchestrator code
with replay semantics; Voluta's is a graph with explicit state channels, which fits an
agent's think-act-observe loop more directly and keeps the loop bound visible.

**vs. rolling your own `while` loop** — Works until you need to answer "what was the state at
step 7?", "how do I resume after the pod restarted?", or "two nodes wrote the same field, now
what?". Those three questions are the entire library.

## Packages

| Package | Role | Status |
|---|---|---|
| `Voluta.Abstractions` | Contracts: channels, checkpoints, `NodeResult`, `Send`, streaming | on `main` |
| `Voluta` | Pregel runtime + InMemory + `Subgraph.AsNode` + `Describe()` | on `main` |
| `Voluta.DependencyInjection` | `AddVoluta` + `AddVolutaCheckpoints` | on `main` |
| `Voluta.Generators` | `[GraphState]` source generator | on `main` |
| `Voluta.Testing` | Test doubles + checkpointer conformance suite | on `main` |
| `Voluta.Checkpoints.File` | JSON file-system checkpointer (`UseFile`) | on `main` |
| `Voluta.Checkpoints.EntityFrameworkCore` | Provider-agnostic EF Core (`UseEntityFrameworkCore<T>`) | on `main` |
| `Voluta.Checkpoints.S3` | AWS S3 / S3-compatible (`UseS3`) | on `main` |
| `Voluta.MicrosoftAi` | `IChatClient` helpers for `Microsoft.Extensions.AI` | on `main` |
| `Voluta.UI` | Ops console: `MapVolutaUI` (inspector / HITL / topology) | on `main` |

**Native AOT** applies to the core tier only — `Voluta`, `Abstractions`, and
`DependencyInjection` are `IsAotCompatible`, with a publish smoke test in `samples/AotSmoke`.
Checkpoint providers (File / EF / S3), UI, and MicrosoftAi are regular-CLR packages and do not
claim AOT.

## Samples

| Sample | What it shows |
|---|---|
| [`01-HelloWorld`](https://github.com/dot-stbl/voluta/tree/main/samples/01-HelloWorld) | Simulated ReAct loop (agent ⇄ tools), streaming updates |
| [`02-InterruptResume`](https://github.com/dot-stbl/voluta/tree/main/samples/02-InterruptResume) | HITL interrupt and `Command` resume |
| [`03-AotSmoke`](https://github.com/dot-stbl/voluta/tree/main/samples/03-AotSmoke) | Native AOT publish smoke test |
| [`04-ReviewBot`](https://github.com/dot-stbl/voluta/tree/main/samples/04-ReviewBot) | CLI review harness: plan → sandboxed tools → review |
| [`05-DocQ`](https://github.com/dot-stbl/voluta/tree/main/samples/05-DocQ) | Docs Q&A over a sandboxed folder |

## What isn't here yet

Stated plainly so you can judge the fit:

- **No published packages.** Source references only until the 0.1 tag.
- **PublicAPI surface can still move** before `v0.1.0` (tracked with PublicApiAnalyzers).
- **UI is a first cut.** `MapVolutaUI` covers inspect / HITL / topology / SSE — not multi-host
  thread discovery or auth.
- **Checkpoint serde** is best-effort JSON for channel values; versioning/evolution is still open.

## Development

```bash
dotnet build voluta.slnx                      # 0 warnings, 0 errors — the gate
dotnet test voluta.slnx                       # xUnit + Shouldly + NSubstitute
dotnet format voluta.slnx --severity hidden   # style drift check
dotnet run -c Release --project benchmarks/Voluta.Benchmarks
```

`TreatWarningsAsErrors` and `EnforceCodeStyleInBuild` are on for every project, so a clean build
*is* the style review. Benchmarks (`LinearInvoke`, `CycleFiveTicks`, `ParallelAppend`,
`CheckpointPutGet`) are not gated on PR CI — see
[#10](https://github.com/dot-stbl/voluta/issues/10).

## Contributing

Small fixes go straight to a PR. For anything non-trivial, open an issue first so we can agree on
the shape before you write it — the runtime's contracts are still moving.

```bash
git config core.hooksPath .githooks   # once per clone
```

The `commit-msg` hook strips AI attribution trailers, so don't hand-add them; commits follow
`[voluta](feat/scope): subject`. AI-generated code is welcome when it's tested and you understand
it. Full setup, conventions, and the PR checklist:
[CONTRIBUTING.md](https://github.com/dot-stbl/voluta/blob/main/CONTRIBUTING.md).

## Inspiration

The execution model comes from [LangGraph](https://github.com/langchain-ai/langgraph) (MIT) —
Pregel-style supersteps, channel/reducer state, checkpoint-first persistence. The API diverges
substantially, and any place where Voluta is wrong about this design space is our own fault,
not theirs.

## License

MIT — see [LICENSE](https://github.com/dot-stbl/voluta/blob/main/LICENSE).
