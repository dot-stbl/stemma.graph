# StemmaGraph

> Stateful, cyclic, durable agent graphs for .NET — inspired by LangGraph,
> redesigned for the .NET runtime.

**Status: scaffolding.** Repo skeleton only — no runtime code yet. See
[CLAUDE.md](CLAUDE.md) for the architecture discussion and [Roadmap](#roadmap)
below.

---

## What is StemmaGraph?

StemmaGraph is a low-level orchestration framework for building long-running,
stateful agents in .NET. You define a graph of nodes (functions, LLM calls,
tools) connected by edges (including **conditional** and **cyclic** edges),
and StemmaGraph runs it with:

- **Cyclic execution** — agents that loop until a condition is met (the
  killer feature missing from most .NET agent frameworks)
- **Durable checkpointing** *(under discussion)* — every step persisted,
  resumable from any checkpoint, time-travel debugging
- **Typed state** — `record`/`class` state, generic constraints, no runtime
  validation
- **Channels + reducers** *(under discussion)* — parallel node updates
  don't clobber each other
- **Streaming** — `IAsyncEnumerable<StateUpdate>` for values / updates /
  events
- **Human-in-the-loop** *(under discussion)* — interrupts that pause and
  resume via `Command`
- **`Microsoft.Extensions.AI` integration** *(planned)* — works with any
  provider through the `IChatClient` abstraction

Items marked *under discussion* depend on what survives the architecture pass
over LangGraph internals. We're not committing to a 1:1 port — features that
make sense for .NET stay, features that don't get cut.

## What it is **not**

- **Not a LangGraph port.** Same conceptual source (cycles + state +
  checkpointing), but .NET-native API: generic state, typed reducers,
  `IAsyncEnumerable`, no `TypedDict` reflection.
- **Not a replacement for Microsoft Agent Framework.** MAF handles
  multi-agent orchestration and function-calling; StemmaGraph handles
  graphs with cycles and durable state. They compose.
- **Not an LLM framework.** StemmaGraph doesn't ship an LLM SDK; it consumes
  `IChatClient` from `Microsoft.Extensions.AI`.

## Packages

**TBD.** Architecture pass and architecture discussion come first. We know:

- There will be a main `StemmaGraph` package (runtime + builder API).
- There will be an `StemmaGraph.Abstractions` package (interfaces only, zero
  transitive dependencies — like `MassTransit.Abstractions`).

What else exists depends on what survives the research pass. The current
skeleton intentionally has only those two.

## Roadmap

- [ ] **Architecture pass** — review LangGraph internals, decide what
      survives the .NET rewrite (cycles? checkpointing? channels? all? some?)
- [ ] **MVP** — runtime + StateGraph + 1 sample (in-memory only)
- [ ] Later — persistence, MAF integration, subgraphs, visualizer, v1.0

## Inspiration

StemmaGraph's design borrows heavily from the open-source
[LangGraph](https://github.com/langchain-ai/langgraph) (Python, MIT) — in
particular its Pregel-style superstep execution, channel/reducer state model,
and checkpoint-first persistence model. The API diverges substantially:
.NET-native generics, typed reducers, `IAsyncEnumerable` streaming, no
`TypedDict` reflection. We are not a port; we are a peer.

## Documentation

- [CLAUDE.md](CLAUDE.md) — architecture discussion, conventions, workflow
- [AGENTS.md](AGENTS.md) — pointer to CLAUDE.md for AI agents
- [CONTRIBUTING.md](CONTRIBUTING.md) — how to set up, build, test, commit
- [docs site](https://stemma.dev) — *coming soon* (custom domain pending)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). PRs welcome — file an issue first if
the change is non-trivial.

## License

[MIT](LICENSE).

---

Built on .NET 10, xUnit + Shouldly + NSubstitute.