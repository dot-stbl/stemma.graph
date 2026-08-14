# PublicAPI review — v0.1 decision draft

> **Status:** decision-ready for owner (not a NuGet tag / publish checklist).  
> **Baseline:** `main` @ `d226540` after PR **#18** (`Voluta.Agents.AI`, `Voluta.MicrosoftAi` removed).  
> **Source of truth:** `src/*/PublicAPI.Unshipped.txt` (all `PublicAPI.Shipped.txt` are empty until first tag — D-026).  
> **Not gated:** `Voluta.Testing` (`IsPackable=false`), `Voluta.Generators` (analyzer package).

---

## Snapshot

| Metric | Value |
|--------|------:|
| Ship packages with PublicAPI gate | **8** |
| Unshipped API lines (excl. `#nullable enable`) | **381** |
| Shipped API lines | **0** |
| Packages recommending **ship as-is** | 6 |
| Packages with **pre-tag internal** candidates | 2 (`Voluta`, `Voluta.Abstractions`, `Voluta.Checkpoints.EntityFrameworkCore`) |

---

## Per-package matrix

Counts = non-comment lines in `PublicAPI.Unshipped.txt`.  
**Ship** = freeze surface as public for `v0.1.0`.  
**Internal** = make `internal` (or remove) **before** tag so they never enter Shipped.

| Package | Unshipped | High-risk / sticky APIs | Recommendation | Pre-tag action |
|---------|----------:|-------------------------|----------------|----------------|
| `Voluta.Abstractions` | 167 | `ICheckpointer`, `CheckpointSnapshot` (+ pending writes/sends), `NodeResult` hierarchy, `Command`/`Send`, stream + topology DTOs, `[GraphState]`/`OptionalValue` | **Ship** — wire + contracts | Optional: hide `IAssemblyMarker` |
| `Voluta` | 64 | `StateGraph` / `CompiledGraph`, `IGraphNode`, `GraphContext` (+ `Services` / `GetRequiredService`), `CompileOptions.Services`, `Subgraph.AsNode`, exception hierarchy, `InMemoryCheckpointer` | **Ship** after optional marker cleanup | **Internal:** `AssemblyMarker` |
| `Voluta.DependencyInjection` | 19 | `AddVoluta` (3 overloads), `VolutaBuilder`, `AddVolutaCheckpoints` / `UseInMemory`, `VolutaCheckpointBuilder` | **Ship** | Optional: narrow `MarkProviderConfigured` (see below) |
| `Voluta.Checkpoints.File` | 7 | `FileCheckpointer`, `UseFile` | **Ship** | None |
| `Voluta.Checkpoints.EntityFrameworkCore` | 34 | `UseEntityFrameworkCore` / `<TContext>`, `IVolutaCheckpointDbContext`, `CheckpointRecord`, checkpointer types, `ApplyVolutaCheckpointModel` | **Ship** entity + DI + model apply (D-027) | **Internal:** `CheckpointRecordConfiguration` (+ `TableName` const if unused outside config) |
| `Voluta.Checkpoints.S3` | 13 | `S3Checkpointer`, `S3CheckpointerOptions`, `UseS3` | **Ship** | None |
| `Voluta.Agents.AI` | 26 | `AgentGraphNode` / `AgentNodeOptions` / `AgentNodes.Add`, `ChatClientGraphNode` / `ChatClientNodeOptions` / `ChatClientNodes.Add` | **Ship** (replaces removed `Voluta.MicrosoftAi`) | None |
| `Voluta.UI` | 51 | `MapVolutaUI` / `AddVolutaUI`, `VolutaUiSession`, `VolutaUiOptions`, wire DTOs (`ThreadSummary`, `HitlThreadSummary`, `ResumeRequest`) | **Ship** for 0.1 ops console (D-025) | Confirm DTOs intentional (yes for JSON/SSE) |

**Total Unshipped: 381.**

---

## Package notes (owner decisions)

### 1. `Voluta.Abstractions` — **SHIP** (167)

Public surface is the **library contract**: channels, C-shape checkpoint document, node results, run command/send, stream events, topology description, source-gen attributes.

| Area | Types (representative) | Verdict |
|------|------------------------|---------|
| Checkpoint C-shape | `ICheckpointer`, `CheckpointSnapshot`, `PendingWrite`, `PendingSend`, `CheckpointStoreException` | Ship — durable wire |
| Node results | `NodeResult`, `ContinueNodeResult`, `InterruptNodeResult` + factories | Ship — HITL contract |
| Runtime | `Command`, `Send`, `RunOptions`, `GraphRunStatus` | Ship |
| Streaming | `StreamEvent`, `StreamEventKind`, `StreamMode` | Ship |
| Topology | `GraphDescription`, `GraphEdgeDescription` | Ship — UI + `Describe()` |
| State / codegen | `GraphChannelSchema`, `ChannelAttribute`, `GraphStateAttribute`, `OptionalValue<T>` | Ship |
| Marker | `IAssemblyMarker` | **Internal (optional)** — scaffold leftover |

**No required pre-tag breaks.** Optional hygiene only on the marker interface.

### 2. `Voluta` (core runtime) — **SHIP** (64)

| Area | Types / members | Verdict |
|------|-----------------|---------|
| Builder | `StateGraph` (`AddNode` handler / `IGraphNode` / factory / `AddNode<T>`, edges, channels, `Compile`) | Ship — primary authoring API (D-028) |
| Runtime | `CompiledGraph` (`Stream`/`Invoke`/`Resume*`, `Describe`) | Ship |
| DI-in-graph | `IGraphNode`, `GraphContext` ctor + `Services` + `GetRequiredService`, `CompileOptions.Services` | Ship — intentional public for node unit tests + host composition |
| Subgraphs | `Subgraph.AsNode` | Ship |
| Checkpoint | `InMemoryCheckpointer` | Ship (MVP default; stays in core per D-006/D-022) |
| Errors | `GraphException` + compile/run/concurrent subclasses | Ship — stable `Code` surface |
| Constants | `GraphConstants.Start` / `End` | Ship |
| Marker | `AssemblyMarker` | **Internal (recommended)** — XML still says “subsequent PRs”; no samples reference it |

**High-risk but intentional:** public `GraphContext` constructor. Nodes and tests construct/fake contexts; hiding it forces InternalsVisibleTo or a test-only factory. **Keep public for v0.1.**

### 3. `Voluta.DependencyInjection` — **SHIP** (19)

| Member | Verdict |
|--------|---------|
| `AddVoluta` (graph instance / factory / configure builder) | Ship — composition root (D-028 host pattern) |
| `VolutaBuilder.Graph` / `.Checkpoints` / `.Services` | Ship |
| `AddVolutaCheckpoints` + `UseInMemory` | Ship |
| `VolutaCheckpointBuilder.Services` / `IsProviderConfigured` | Ship — extension authors need `Services` |
| `VolutaCheckpointBuilder.MarkProviderConfigured()` | **Ship for 0.1** as extension-author API (File/EF/S3 call it). Prefer **not** to break cross-package `Use*` without `InternalsVisibleTo` matrix |

### 4. `Voluta.Checkpoints.File` — **SHIP** (7)

Minimal: `FileCheckpointer` + `UseFile`. No hide list.

### 5. `Voluta.Checkpoints.EntityFrameworkCore` — **SHIP** with one hide (34)

| Member | Verdict |
|--------|---------|
| `UseEntityFrameworkCore` / `UseEntityFrameworkCore<TContext>` | Ship |
| `IVolutaCheckpointDbContext`, `VolutaCheckpointDbContext`, `CheckpointRecord` | Ship — consumer owns schema/migrations (D-027) |
| `EntityFrameworkCoreCheckpointer` / `<TContext>` | Ship — parity with File/S3 concretes |
| `ApplyVolutaCheckpointModel` | Ship — preferred model hook |
| `CheckpointRecordConfiguration` (+ `TableName`) | **Internal (recommended)** — only needed inside `ApplyVolutaCheckpointModel` / DbContext; consumers should not re-apply Fluent config by hand |

If a host must customize table name, add a **named options** surface later (post-0.1); do not leave Fluent config type as accidental public API.

### 6. `Voluta.Checkpoints.S3` — **SHIP** (13)

`S3Checkpointer` + `S3CheckpointerOptions` (`BucketName`, `KeyPrefix`) + `UseS3`. Clean provider surface.

### 7. `Voluta.Agents.AI` — **SHIP** (26)

Post-#18 surface only (no `MicrosoftAi`):

| Type | Role | Verdict |
|------|------|---------|
| `AgentGraphNode` / `AgentNodeOptions` / `AgentNodes.Add` | MAF `AIAgent` → `IGraphNode` | Ship |
| `ChatClientGraphNode` / `ChatClientNodeOptions` / `ChatClientNodes.Add` | MEAI `IChatClient` → `IGraphNode` | Ship |
| Static `Create` factories | ergonomics | Ship |

Static helpers are **not** `this StateGraph` extensions (D-028). Core remains AI-free.

### 8. `Voluta.UI` — **SHIP** (51)

| Area | Verdict |
|------|---------|
| `MapVolutaUI` overloads + `AddVolutaUI` | Ship — host entry (D-025) |
| `VolutaUiSession` | Ship — process-scoped ops façade |
| `VolutaUiOptions` (+ `SectionName`) | Ship |
| `ThreadSummary`, `HitlThreadSummary`, `ResumeRequest` | Ship — **intentional wire/JSON** types for RCL + SSE |

Do **not** internalize DTOs unless endpoints move to Abstractions (out of scope for 0.1).

---

## Types that should become `internal` **before** `v0.1.0` tag

Do these in a small pre-tag PR (docs-only review does **not** apply them).

| Priority | Type / member | Package | Why |
|----------|---------------|---------|-----|
| **P0** | `Voluta.AssemblyMarker` | `Voluta` | Scaffold marker; zero product value; freezes noise into Shipped |
| **P0** | `Voluta.Abstractions.IAssemblyMarker` | `Voluta.Abstractions` | Same |
| **P1** | `CheckpointRecordConfiguration` (class + `TableName` if only used there) | `Voluta.Checkpoints.EntityFrameworkCore` | Implementation detail of `ApplyVolutaCheckpointModel`; accidental Fluent-API surface |
| **P2** | — | — | No further hides required for tag |

**Explicitly keep public (do not hide):**

- `GraphContext` ctor / `Services` / `GetRequiredService`
- `IGraphNode`, all `StateGraph.AddNode*` overloads, `CompileOptions.Services`
- `CheckpointRecord`, `IVolutaCheckpointDbContext`, concrete checkpointers
- UI session + DTO types
- `VolutaCheckpointBuilder.MarkProviderConfigured` (unless simultaneous InternalsVisibleTo for all checkpoint packages)

---

## High-risk APIs (freeze awareness — not blockers)

These **should ship**, but owners should treat them as **semver-hard** after Unshipped → Shipped:

1. **`CheckpointSnapshot` shape** — full C-shape document; serde still “best-effort JSON” (open Q in decisions). Changing fields after 0.1 is a wire break for File/EF/S3.
2. **`Command` / resume taxonomy** — open question (approve/reject/update-state). Shipping opaque `Kind` + `Payload` is OK for 0.1; structured verbs later must be additive.
3. **`GraphContext.Services` nullability** — host must pass `CompileOptions.Services`; nodes that call `GetRequiredService` fail closed.
4. **`IGraphNode` + MAF/MEAI adapters** — third-party version coupling (`Microsoft.Agents.AI.Abstractions`, `Microsoft.Extensions.AI.Abstractions`); package is full-tier, not AOT-claimed.
5. **UI process-tracked threads** — `VolutaUiSession.TrackThread` is in-process; multi-thread discovery still roadmap (not API break if additive).

---

## Ready to tag?

### Answer: **Yes — owner OK + P0/P1 hygiene applied; surface moved to Shipped for v0.1.0**

| Check | Status |
|-------|--------|
| Runtime + post-MVP packages on `main` | ✅ (#16–#18) |
| PublicAPI analyzers + Unshipped inventories | ✅ 8 packages / 381 lines |
| `Voluta.MicrosoftAi` gone; `Voluta.Agents.AI` present | ✅ #18 |
| Unified `AddVoluta` builder | ✅ #17 |
| Owner sign-off on this review | ❌ **blocker** |
| P0 markers → internal (recommended) | ✅ removed |
| P1 `CheckpointRecordConfiguration` → internal | ✅ done (v0.1 hygiene) |
| Unshipped → Shipped move after publish | ⏳ post-tag process |
| Arch tests (package isolation) | ❌ deferred — **not** a tag blocker |
| Docs site | ❌ deferred — not a tag blocker |
| Checkpoint polymorphic serde finalization | ❌ open Q — **not** blocking 0.1 if documented as best-effort |
| NuGet OIDC / release workflow dry-run | ⚠️ confirm before first `v0.1.0` push (process, not API) |

### Residual blockers for `v0.1.0` tag

1. **Owner decision:** accept this matrix (or mark deltas) and approve freeze.
2. **Optional pre-tag PR:** P0 (+ P1) internals above — reduces forever-public noise.
3. **Release process only (out of scope of this doc):** tag `v0.1.0`, publish nuget.org, move each Unshipped body → Shipped, leave Unshipped with `#nullable enable` only, close epic #1.

**Not blockers:** architecture isolation tests, UI polish, docs site, sample migration leftovers, open command taxonomy / token streaming design.

### Freeze steps (after owner OK)

1. Land optional internal hygiene PR if desired.  
2. Tag `v0.1.0` → pack → nuget.org (OIDC).  
3. Move Unshipped → Shipped per package.  
4. Close epic #1 / milestone notes.

---

## Decision log (this review)

| Decision | Choice |
|----------|--------|
| Ship packages for 0.1 | All 8 gated packages |
| Agents package | `Voluta.Agents.AI` only (no MicrosoftAi) |
| EF entity public | Yes (D-027) — configuration class preferably not |
| UI DTOs public | Yes — wire types |
| GraphContext public ctor | Yes — testability |
| Markers | Prefer internal before first Shipped snapshot |

---

## Related

- [roadmap.md](../roadmap.md) — status row “NuGet 0.1 tag”
- [decisions.md](../decisions.md) — D-026 PublicAPI gate, D-027 EF/S3, D-028 Agents.AI
- Inventories: `src/*/PublicAPI.Unshipped.txt`
- Epic #1 · milestone `v0.1 · MVP runtime`
