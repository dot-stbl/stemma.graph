## 1. Align repo planning docs (no runtime code)

- [ ] 1.1 Update `.agents/decisions.md` with session decisions (Pregel C, channels, C-checkpoint, NodeResult HITL, stream modes, Both hosting, Testing package, MVP B)
- [ ] 1.2 Update `.agents/roadmap.md` MVP section to match honest Pregel + InMemory; list deferred Send/subgraphs/EF/UI
- [ ] 1.3 Add short pointer in `CLAUDE.md` / README status to this OpenSpec change path
- [ ] 1.4 Optionally copy a one-page overview into `.agents/architecture/00-overview.md` linking to `openspec/changes/architecture-runtime-core/`

## 2. Validate and archive readiness

- [x] 2.1 Run `openspec validate architecture-runtime-core --strict` and fix any spec format issues
- [ ] 2.2 Owner review of proposal + design open questions (Command taxonomy, token streaming, InMemory export)
- [ ] 2.3 After approval: archive change into main `openspec/specs/*` when ready (separate step; not blocking implement PRs)
- [x] 2.4 Add gap specs: graph-topology, error-cancellation, quality-engineering; extend checkpoint get/list

## 3. Abstractions skeleton (first code change — later apply)

- [ ] 3.1 Define public contracts in `StemmaGraph.Abstractions`: channels, writes, NodeResult, CheckpointSnapshot (C-shape), ICheckpointer, stream events, Command
- [ ] 3.2 Add PublicAPI Unshipped tracking once types exist
- [ ] 3.3 Unit tests for pure types/helpers only (no full runtime yet)

## 4. Runtime MVP (Pregel + InMemory)

- [ ] 4.1 Channel implementations: LastValue, Append/binop minimum
- [ ] 4.2 StateGraph builder + compile → CompiledGraph (topology validation per graph-topology)
- [ ] 4.3 Superstep loop: prepare ready, parallel run, barrier, apply_writes, versions
- [ ] 4.4 InMemory checkpointer (full C-shape fields used by MVP paths; get miss + optional list)
- [ ] 4.5 Interrupt/resume path via NodeResult + ResumeAsync
- [ ] 4.6 Stream modes values/updates/events + InvokeAsync convenience
- [ ] 4.7 DI registration helpers (Both) + sample without DI
- [ ] 4.8 Failure + cancellation paths per error-cancellation (throw → failed; cancel ≠ interrupt)
- [ ] 4.9 Unit scenario matrix from quality-engineering (all listed cases green)

## 5. Source-gen state (MVP skeleton)

- [ ] 5.1 Generator project: `[GraphState]` → Schema + Update + ToWrites
- [ ] 5.2 Sample using generated state for ReAct-style loop
- [ ] 5.3 Document fluent escape hatch without generator

## 6. StemmaGraph.Testing package

- [ ] 6.1 Create `StemmaGraph.Testing` project + slnx registration
- [ ] 6.2 RecordingCheckpointer + FaultInjectingCheckpointer
- [ ] 6.3 Stream capture helper + graph fixtures (linear, cycle, interrupt, multi-ready)
- [ ] 6.4 Checkpoint conformance suite running on InMemory in CI

## 7. Samples and ship gate

- [ ] 7.1 Replace `samples/01-HelloWorld` with real ReAct-style in-memory sample
- [ ] 7.2 Sample: interrupt + resume
- [ ] 7.3 `dotnet build` / `dotnet test` green; document 0.1.0 non-goals (no EF/Send/UI)
- [ ] 7.4 Tag/publish decision (owner) after PublicAPI review

## 8. Benchmarks and CI hardening

- [ ] 8.1 Add `benchmarks/StemmaGraph.Benchmarks` (BenchmarkDotNet): superstep overhead, cycle, parallel+Append, InMemory put/get
- [ ] 8.2 CI: expand path filters (`tests/**`, `openspec/**`, `benchmarks/**`); pack smoke; openspec validate when openspec changes
- [ ] 8.3 Ensure Testing / samples are not accidentally published (`IsPackable` / allowlist)
- [ ] 8.4 Optional non-gating bench job (workflow_dispatch or schedule)
- [ ] 8.5 Architecture tests when multi-project: Abstractions isolation, core ↛ EF/S3

## 9. Explicitly deferred (do not implement in MVP apply)

- [ ] 9.1 Send fan-out execution (keep types reserved per send-subgraphs spec)
- [ ] 9.2 Subgraph compose API
- [ ] 9.3 EF Core / S3 / File checkpointer packages
- [ ] 9.4 Microsoft.Extensions.AI integration package
- [ ] 9.5 UI (HTML/Razor) package
