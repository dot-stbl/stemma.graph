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

**Stateful agent graphs for .NET** — cycles, typed channels, checkpoints, streaming, and
human-in-the-loop. You describe nodes and edges; Voluta runs them in Pregel-style supersteps and
can pause a thread for days, then resume in another process.

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

A ReAct-style loop (agent ⇄ tools) with no LLM — pure simulation so you can run it offline.
Real output of `dotnet run --project samples/HelloWorld`:

```text
  ◆  voluta · HelloWorld

  simulated ReAct · agent ⇄ tools · no LLM
  thread   react-sample-1
  stream   Updates
  ────────────────────────────────────────
  [agent] round 0 · requesting tools
  · step 1  Updates  [agent]
      status ← tools
      messages ← agent: call get_weather (round 1)
  [tools] executing simulated tool · round 1
  · step 2  Updates  [tools]
      tool_rounds ← 1
      messages ← tools: observation — temp=12C (round 1)
      status ← agent
  …
  [agent] enough tool data · finishing
  · step 5  Updates  [agent]
      status ← done
      messages ← agent: final answer — cloudy, 12°C in Oslo
  · step 5  End  [—]

  ▸ result
  status      Done
  ✓ done
```

The loop exists because one edge is **conditional**. `messages` accumulates (`Append`);
`status` replaces (`LastValue`):

```csharp
var graph = new StateGraph()
    .AddChannel("messages", ChannelKind.Append)
    .AddChannel("status", ChannelKind.LastValue)
    .AddChannel("tool_rounds", ChannelKind.LastValue)
    .AddNode("agent", AgentNodeAsync)
    .AddNode("tools", ToolsNodeAsync)
    .AddEdge(GraphConstants.Start, "agent")
    .AddConditionalEdges(
        "agent",
        static context => context.Read<string>("status") == "tools" ? "tools" : GraphConstants.End)
    .AddEdge("tools", "agent")
    .Compile(checkpointer, new CompileOptions { RecursionLimit = 32 });

await foreach (var item in graph.StreamAsync(
                   input,
                   new RunOptions { ThreadId = "react-1", StreamMode = StreamMode.Updates }))
{
    // item.Kind, item.NodeNames, item.Writes
}
```

<details>
<summary><strong>Human-in-the-loop — interrupt, then resume (even later, another process)</strong></summary>

A node returns `NodeResult.Interrupt` instead of writes. The run stops, the checkpoint holds the
payload, and `ResumeInvokeAsync` continues with a `Command`. Real output of
`samples/InterruptResume`:

```text
  ▸ invoke · expect interrupt

  · step 0  Start  [—]
  [gate] interrupting for human approval
  · step 1  Interrupt  [gate]
      payload ← { action = transfer, amount = 50, currency = USD }
  checkpoint  Interrupted

  ▸ resume · Command.Kind = approve

  [gate] resumed · payload=ok · approving
  · step 2  End  [—]
  status      Done
  ✓ done
```

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

// first process
var terminal = await graph.InvokeAsync(input, new RunOptions { ThreadId = "hitl-1" });
// terminal.Kind == StreamEventKind.Interrupt

// later — same or new process, same checkpointer root / store
var done = await graph.ResumeInvokeAsync(
    "hitl-1",
    new Command
    {
        Kind = "approve",
        Payload = "ok",
        // optional: patch channels before the node re-runs
        Values = new Dictionary<string, object?> { ["decision"] = "go" },
    });
```

</details>

<details>
<summary><strong>Typed state with <code>[GraphState]</code> (source generator)</strong></summary>

String channel names work. When you want compile-time names and updates that only write what you
set:

```csharp
[GraphState]
public partial class ReviewState
{
    [Channel(ChannelKind.Append)]
    public IList<object?> Notes { get; set; } = new List<object?>();

    [Channel(ChannelKind.LastValue)]
    public string? Verdict { get; set; }
}

var graph = new StateGraph()
    .AddChannels(ReviewState.CreateSchema())
    .AddNode(
        "review",
        static (context, cancellationToken) => Task.FromResult<NodeResult>(
            NodeResult.Continue(
                new ReviewState.ReviewStateUpdate { Verdict = "approved" }.ToWrites())))
    .AddEdge(GraphConstants.Start, "review")
    .AddEdge("review", GraphConstants.End)
    .Compile(new InMemoryCheckpointer());
```

Unset properties emit **no** write. An explicit `null` is a clear. Interface-typed properties use
`OptionalValue<T>.Of(value)`.

</details>

<details>
<summary><strong>Durable file checkpoint (survive process restart)</strong></summary>

```csharp
var checkpointer = new FileCheckpointer("/var/lib/voluta/threads"); // or any root path
var graph = new StateGraph()
    // … nodes …
    .Compile(checkpointer);

// process A
await graph.InvokeAsync(input, new RunOptions { ThreadId = "order-9" });
// → Interrupted

// process B (new host, same root)
var graph2 = /* recompile same topology */ .Compile(new FileCheckpointer("/var/lib/voluta/threads"));
await graph2.ResumeInvokeAsync("order-9", new Command { Kind = "approve", Payload = "ok" });
```

Values serialize with `System.Text.Json` — prefer JSON-friendly types (strings, numbers, lists of
primitives). For tests, `InMemoryCheckpointer` is enough; every storage implements
`ICheckpointer` and can run `CheckpointerConformance.RunAllAsync`.

</details>

<details>
<summary><strong>Fan-out with <code>Send</code> / subgraphs</strong></summary>

```csharp
// Map one item to many dynamic tasks (runtime-scheduled nodes)
return NodeResult.ContinueWithSends(
    new Send("worker", payload: orderLine1),
    new Send("worker", payload: orderLine2));

// Nest a compiled graph as a single node
.AddNode("child", Subgraph.AsNode(childCompiledGraph))
```

`CompiledGraph.Describe()` returns topology (nodes, edges, channels) for the ops UI or docs.

</details>

<details>
<summary><strong>Host with DI + ops console</strong></summary>

```csharp
// Program.cs
builder.Services.AddVoluta(sp =>
{
    var checkpointer = sp.GetRequiredService<ICheckpointer>();
    return new StateGraph()
        // …
        .Compile(checkpointer);
});

// Optional ops UI (ASP.NET)
var session = new VolutaUiSession(graph, checkpointer);
builder.Services.AddVolutaUI(session);
app.MapVolutaUI(options => options.PathPrefix = "/voluta");
// → http://localhost:5188/voluta  (see samples/UiHost)
```

</details>

<details>
<summary><strong>Testing without fighting the runtime</strong></summary>

`Voluta.Testing` ships doubles you’d otherwise write by hand:

| Helper | Role |
|--------|------|
| `RecordingCheckpointer` | Records every Put/Get/List |
| `FaultInjectingCheckpointer` | Fails the *n*-th write |
| `CheckpointerConformance.RunAllAsync` | Suite every `ICheckpointer` must pass |
| `GraphFixtures.Linear()` / `.Cycle()` / `.Interrupt()` | Ready graphs |
| `StreamCapture` | Drain a stream in tests |

Unit tests cover linear/conditional/cycle, fan-out, Send, HITL (`Command.Values`, resume payload),
stream modes (Updates / Events), Append flatten vs string, LastValue sequential overwrite, and
File rehydrate + resume.

```bash
dotnet test voluta.slnx
```

</details>

<details>
<summary><strong>Sample: Hybrid-style marketing desk (mock tools)</strong></summary>

Not a product agent — a demo that **exercises the graph** against Hybrid console.platform nouns
(Campaign, SSP, DirectDeal, AdLibrary):

```bash
# terminal 1
dotnet run --project samples/MockAdMcp          # http://localhost:5190

# terminal 2
dotnet run --project samples/MarketingAgent -- --offline
```

Flow: `brief → creative → setup (create RK → SSP → banner → Active) → review`. Tools are a
simplified HTTP tools surface for demos — not the official MCP SDK and not live Hybrid API.

</details>

## Why Voluta

- **Cycles are the point** — think → act → observe is a loop. Conditional edges + `RecursionLimit`
  make loops terminate on purpose.
- **The run outlives the process** — every superstep can checkpoint. Interrupt, inspect, resume, or
  replay from storage; `ICheckpointer` is the only seam.
- **Multi-writer state is defined** — same-superstep writes go through reducers (`Append` /
  `LastValue`). Concurrent `LastValue` writers fail fast instead of last-write-wins races.
- **Core stays small** — `Voluta` + Abstractions + DI are `IsAotCompatible`, zero third-party deps
  on the hot path. No LLM SDK, no logging framework required to run a graph.

Voluta is **orchestration**, not a Claude Code / OpenCode clone. You bring tools, MCP, prompts, and
policy; Voluta runs the stateful graph.

## Quick Start

**Requires .NET 10 SDK** (`dotnet --version` ≥ 10.0.100).

```bash
git clone https://github.com/dot-stbl/voluta.git
cd voluta
dotnet build voluta.slnx
dotnet run --project samples/HelloWorld
dotnet run --project samples/InterruptResume
```

Reference from your app:

```bash
dotnet add reference path/to/voluta/src/Voluta/Voluta.csproj
```

### Minimal complete program

Writer + critic loop until score ≥ 8 — prints `End after N supersteps`:

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
    .AddChannel("draft", ChannelKind.LastValue)
    .AddChannel("notes", ChannelKind.Append)
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

Swap `InvokeAsync` for `StreamAsync` (`StreamMode.Values` / `Updates` / `Events`). Under a host:
`services.AddVoluta(provider => …)`.

### Chat helpers (optional)

```csharp
// Voluta.MicrosoftAi — thin IChatClient wrapper for nodes
var text = await ChatClientNode.CompleteTextAsync(chatClient, messages, cancellationToken: ct);
// or write assistant text into a channel:
return await ChatClientNode.CompleteToChannelAsync(
    chatClient, "answer", messages, cancellationToken: ct);
```

## Host DI — `AddVoluta`

One composition root: checkpoints + graph (and later UI hooks). Prefer this over raw
`AddSingleton<ICheckpointer>(…)` / ad-hoc factories.

```csharp
// Recommended: checkpoints + graph in one call
services.AddVoluta(v =>
{
    v.Checkpoints.UseInMemory(); // or UseFile / UseEntityFrameworkCore / UseS3
    v.Graph((sp, checkpointer) => new StateGraph()
        // …nodes, edges, channels…
        .Compile(checkpointer));
});

// Graph only (already compiled, or factory that owns its own checkpointer)
services.AddVoluta(prebuiltGraph);
services.AddVoluta(sp => BuildGraph(sp));

// Checkpoints only (resolve ICheckpointer yourself)
services.AddVolutaCheckpoints(c => c.UseFile("./.voluta/checkpoints"));
```

### Checkpoint providers (`v.Checkpoints.Use*`)

Exactly one `Use*` per host:

```csharp
v.Checkpoints.UseInMemory();
v.Checkpoints.UseFile("./.voluta/checkpoints");

// EF — register IDbContextFactory first (any provider: Npgsql, SqlServer, SQLite, …)
services.AddDbContextFactory<AppDbContext>(o => o.UseNpgsql(connectionString));
v.Checkpoints.UseEntityFrameworkCore<AppDbContext>();

// S3 — register IAmazonS3 first
services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(RegionEndpoint.EUCentral1));
v.Checkpoints.UseS3(o =>
{
    o.BucketName = "voluta";
    o.KeyPrefix = "runs";
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

One tick, in order: collect ready nodes → run them **concurrently** → **barrier** → merge writes
through channel reducers → checkpoint → evaluate edges.

- Nodes in the same superstep **never** see each other’s writes (barrier isolation).
- Conditional edges run **after** the merge, on committed state.

Contracts: [`openspec/specs/`](openspec/specs/) (12 capabilities).

## How it compares

**vs. [LangGraph](https://github.com/langchain-ai/langgraph)** (Python) — Origin of the execution
model (supersteps, channels, checkpoint-first). Voluta rebuilds the surface on .NET generics, typed
reducers, and `IAsyncEnumerable`. Not a port; a peer.

**vs. Microsoft Agent Framework** — Better for multi-agent chat and function calling. It does not
give cyclic graphs with durable per-thread state. Compose: run MAF agents *inside* Voluta nodes.

**vs. Durable Functions / Durable Task** — Stronger storage ecosystem for business workflows.
Orchestrator + replay vs graph + explicit channels — Voluta fits think-act-observe loops more
directly.

**vs. a hand-rolled `while`** — Works until “state at step 7?”, “resume after restart?”, or “two
nodes wrote the same field”. Those three questions *are* the library.

## Packages

How-to for checkpoints (DI `Use*` + host DbContext) is under
[Durable checkpoints](#durable-checkpoints). Behavior contracts:
[`openspec/specs/checkpoint/`](openspec/specs/checkpoint/). Until NuGet publishes,
browse source under [`src/`](src/) (each package is one folder; no per-package README yet).

| Package | Role | Source |
|---------|------|--------|
| `Voluta.Abstractions` | Contracts: channels, checkpoints, `NodeResult`, `Send`, streaming | [`src/Voluta.Abstractions`](src/Voluta.Abstractions/) |
| `Voluta` | Pregel runtime + InMemory + `Subgraph.AsNode` + `Describe()` | [`src/Voluta`](src/Voluta/) |
| `Voluta.DependencyInjection` | `AddVoluta(v => { v.Checkpoints…; v.Graph… })` | [`src/Voluta.DependencyInjection`](src/Voluta.DependencyInjection/) |
| `Voluta.Generators` | `[GraphState]` source generator | [`src/Voluta.Generators`](src/Voluta.Generators/) |
| `Voluta.Testing` | Test doubles + checkpointer conformance suite | [`src/Voluta.Testing`](src/Voluta.Testing/) |
| `Voluta.Checkpoints.File` | JSON file-system checkpointer (`UseFile`) | [`src/Voluta.Checkpoints.File`](src/Voluta.Checkpoints.File/) |
| `Voluta.Checkpoints.EntityFrameworkCore` | Provider-agnostic EF Core (`UseEntityFrameworkCore<T>`) | [`src/Voluta.Checkpoints.EntityFrameworkCore`](src/Voluta.Checkpoints.EntityFrameworkCore/) |
| `Voluta.Checkpoints.S3` | AWS S3 / S3-compatible (`UseS3`) | [`src/Voluta.Checkpoints.S3`](src/Voluta.Checkpoints.S3/) |
| `Voluta.MicrosoftAi` | `IChatClient` helpers for `Microsoft.Extensions.AI` | [`src/Voluta.MicrosoftAi`](src/Voluta.MicrosoftAi/) |
| `Voluta.UI` | Ops console: `MapVolutaUI` (inspector / HITL / topology) | [`src/Voluta.UI`](src/Voluta.UI/) |

**Native AOT** applies to the core tier only — `Voluta`, `Abstractions`, and
`DependencyInjection` are `IsAotCompatible`, with a publish smoke test in `samples/AotSmoke`.
Checkpoint providers (File / EF / S3), UI, and MicrosoftAi are regular-CLR packages and do not
claim AOT.

## Samples

| Sample | What it shows |
|--------|----------------|
| [`HelloWorld`](samples/HelloWorld/) | Cyclic agent ⇄ tools, `StreamMode.Updates` |
| [`InterruptResume`](samples/InterruptResume/) | HITL interrupt + `Command` resume |
| [`AotSmoke`](samples/AotSmoke/) | Native AOT publish smoke |
| [`ReviewBot`](samples/ReviewBot/) | CLI: plan → sandbox tools → review (+ optional HITL) |
| [`DocQ`](samples/DocQ/) | Docs Q&A over a sandboxed folder |
| [`MarketingAgent`](samples/MarketingAgent/) | Hybrid desk setup over mock tools |
| [`MockAdMcp`](samples/MockAdMcp/) | Hybrid-shaped tool server (Campaign / SSP / deal / AdLibrary) |
| [`UiHost`](samples/UiHost/) | ASP.NET host for `MapVolutaUI` |

Index: [`samples/README.md`](samples/README.md).

```bash
dotnet run --project samples/HelloWorld
dotnet run --project samples/InterruptResume
dotnet run --project samples/ReviewBot -- --offline --root .
dotnet run --project samples/DocQ -- --offline --root . --question "What is Voluta?"
dotnet run --project samples/MockAdMcp          # :5190
dotnet run --project samples/MarketingAgent -- --offline
dotnet run --project samples/UiHost             # http://localhost:5188/voluta
```

## What isn't here yet

Stated plainly so you can judge the fit:

- **No published packages.** Source references only until the 0.1 tag.
- **PublicAPI surface can still move** before `v0.1.0` (tracked with PublicApiAnalyzers).
- **UI is a first cut.** `MapVolutaUI` covers inspect / HITL / topology / SSE — not multi-host
  thread discovery or auth.
- **Checkpoint serde** is best-effort JSON for channel values; versioning/evolution is still open.
- **No first-party MCP client/server** — samples use a demo HTTP tools surface; real MCP is
  `ModelContextProtocol` (+ AspNetCore) on top of Voluta.
- **No built-in coding agent** (bash/edit/permissions) — Voluta is the graph runtime, not Claude Code.

## Development

```bash
dotnet build voluta.slnx                      # 0 warnings, 0 errors — the gate
dotnet test  voluta.slnx                      # xUnit + Shouldly + NSubstitute
dotnet format voluta.slnx --severity hidden   # style drift
dotnet run -c Release --project benchmarks/Voluta.Benchmarks
```

`TreatWarningsAsErrors` + `EnforceCodeStyleInBuild` are on. Specs:
[`openspec/specs/`](openspec/specs/). Agent handbook: [`CLAUDE.md`](CLAUDE.md).

## Contributing

Small fixes → PR. Non-trivial changes → issue first (contracts still move).

```bash
git config core.hooksPath .githooks   # once per clone
```

Commit shape: `[voluta](feat/scope): subject`. Details:
[CONTRIBUTING.md](CONTRIBUTING.md).

## Inspiration

Execution model from [LangGraph](https://github.com/langchain-ai/langgraph) (MIT) — supersteps,
channel/reducer state, checkpoint-first persistence. The API diverges substantially; design
mistakes are ours, not theirs.

## License

MIT — see [LICENSE](LICENSE).
