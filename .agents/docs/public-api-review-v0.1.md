# PublicAPI review — v0.1 freeze (draft)

> Owner checklist before tagging `v0.1.0`. All ship packages track surface in
> `PublicAPI.Unshipped.txt` (Shipped empty until tag). After publish: move Unshipped → Shipped.

## Packages in gate

| Package | Unshipped focus | Ship as-is? |
|---------|-----------------|-------------|
| `Voluta.Abstractions` | Checkpoint C-shape, `NodeResult`, stream events, `Command`/`Send` | **Yes** — wire contract |
| `Voluta` | `StateGraph`, `CompiledGraph`, `GraphContext`, **new** `IGraphNode` / DI | **Yes** after this PR |
| `Voluta.DependencyInjection` | `AddVoluta` / `VolutaBuilder` | **Yes** |
| `Voluta.Checkpoints.*` | File / EF / S3 + `Use*` | **Yes** (EF entity public — intentional D-027) |
| `Voluta.Agents.AI` | `AgentGraphNode`, `ChatClientGraphNode`, helpers | **Yes** (replaces removed `Voluta.MicrosoftAi`) |
| `Voluta.UI` | `MapVolutaUI`, session DTOs | **Yes** for 0.1 ops console |

**Not gated:** `Voluta.Testing`, `Voluta.Generators`.

## New surface in this PR (must review)

### Core

- `IGraphNode.InvokeAsync`
- `StateGraph.AddNode<TNode>`, `AddNode(IGraphNode)`, `AddNode(Func<IServiceProvider, IGraphNode>)`
- `CompileOptions.Services`
- `GraphContext` ctor + `Services` + `GetRequiredService<T>()`

### Voluta.Agents.AI

- `AgentGraphNode` / `AgentNodeOptions` / `AgentNodes.Add`
- `ChatClientGraphNode` / `ChatClientNodeOptions` / `ChatClientNodes.Add`

## Recommended pre-tag hygiene (optional)

1. Consider `internal` for EF `CheckpointRecordConfiguration` if consumers never need it.
2. Confirm UI DTOs (`ResumeRequest`, summaries) are intentional public wire types.
3. Plan migration of samples from `IChatCompletionClient` → MEAI + Agents.AI (post-0.1 OK).

## Freeze steps

1. Owner OK on Unshipped lists (this doc + files under `src/*/PublicAPI.Unshipped.txt`).
2. Tag `v0.1.0`, publish nuget.org.
3. Move each Unshipped body → Shipped; leave Unshipped with `#nullable enable` only.
4. Close epic #1.
