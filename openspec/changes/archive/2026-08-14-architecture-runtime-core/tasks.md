## 1. Align repo planning docs (no runtime code)

- [x] 1.1 Update `.agents/decisions.md` with session decisions (Pregel C, channels, C-checkpoint, NodeResult HITL, stream modes, Both hosting, Testing package, MVP B)
- [x] 1.2 Update `.agents/roadmap.md` MVP section to match honest Pregel + InMemory; list deferred / shipped post-0.1
- [x] 1.3 Add short pointer in `CLAUDE.md` / README status to this OpenSpec change path
- [ ] 1.4 Optionally copy a one-page overview into `.agents/architecture/00-overview.md` linking to `openspec/changes/architecture-runtime-core/`

## 2. Validate and archive readiness

- [x] 2.1 Run `openspec validate architecture-runtime-core --strict` and fix any spec format issues
- [ ] 2.2 Owner review of proposal + design open questions (Command taxonomy, token streaming, InMemory export)
- [ ] 2.3 After approval: archive change into main `openspec/specs/*` when ready (separate step; not blocking implement PRs)
- [x] 2.4 Add gap specs: graph-topology, error-cancellation, quality-engineering; extend checkpoint get/list

## 3. Abstractions skeleton (first code change — later apply)

- [x] 3.1 Define public contracts in `StemmaGraph.Abstractions`: channels, writes, NodeResult, CheckpointSnapshot (C-shape), ICheckpointer, stream events, Command, Send, PendingSend, GraphDescription
- [ ] 3.2 Add PublicAPI Unshipped tracking once types exist
- [x] 3.3 Unit tests for pure types/helpers only (no full runtime yet)

## 4. Runtime MVP (Pregel + InMemory)

- [x] 4.1 Channel implementations: LastValue, Append/binop minimum
- [x] 4.2 StateGraph builder + compile → CompiledGraph (topology validation per graph-topology)
- [x] 4.3 Superstep loop: prepare ready, parallel run, barrier, apply_writes, versions
- [x] 4.4 InMemory checkpointer (full C-shape fields used by MVP paths; get miss + optional list)
- [x] 4.5 Interrupt/resume path via NodeResult + ResumeAsync
- [x] 4.6 Stream modes values/updates/events + InvokeAsync convenience
- [x] 4.7 DI registration helpers (Both) + sample without DI (`StemmaGraph.DependencyInjection`)
- [x] 4.8 Failure + cancellation paths per error-cancellation (throw → failed; cancel ≠ interrupt)
- [x] 4.9 Unit scenario matrix from quality-engineering (all listed cases green)

## 5. Source-gen state (MVP skeleton)

- [x] 5.1 Generator project: `[GraphState]` → Schema + Update + ToWrites
- [ ] 5.2 Sample using generated state for ReAct-style loop (HelloWorld still hand-channels)
- [x] 5.3 Document fluent escape hatch without generator

## 6. StemmaGraph.Testing package

- [x] 6.1 Create `StemmaGraph.Testing` project + slnx registration
- [x] 6.2 RecordingCheckpointer + FaultInjectingCheckpointer
- [x] 6.3 Stream capture helper + graph fixtures (linear, cycle, interrupt, multi-ready)
- [x] 6.4 Checkpoint conformance suite running on InMemory in CI

## 7. Samples and ship gate

- [x] 7.1 Replace `samples/01-HelloWorld` with real ReAct-style in-memory sample
- [x] 7.2 Sample: interrupt + resume (`02-InterruptResume`)
- [x] 7.3 `dotnet build` / `dotnet test` green; AOT smoke (`03-AotSmoke`); harness samples 04/05
- [ ] 7.4 Tag/publish decision (owner) after PublicAPI review

## 8. Benchmarks and CI hardening

- [x] 8.1 Add `benchmarks/StemmaGraph.Benchmarks` (BenchmarkDotNet): superstep overhead, cycle, parallel+Append, InMemory put/get
- [ ] 8.2 CI: expand path filters (`tests/**`, `openspec/**`, `benchmarks/**`); pack smoke; openspec validate when openspec changes
- [x] 8.3 Ensure Testing / samples are not accidentally published (`IsPackable` / allowlist)
- [ ] 8.4 Optional non-gating bench job (workflow_dispatch or schedule)
- [ ] 8.5 Architecture tests when multi-project: Abstractions isolation, core ↛ EF/S3/UI

## 9. Post-MVP (implemented on main beyond original deferral)

Originally listed as deferred for MVP cut; **shipped on main** after 0.1 runtime:

- [x] 9.1 Send fan-out execution (`Send`, `ContinueWithSends`, PUSH ready tasks, `PendingSends` on checkpoint)
- [x] 9.2 Subgraph compose helper (`Subgraph.AsNode`) + topology export (`CompiledGraph.Describe`)
- [x] 9.3 File checkpointer package (`StemmaGraph.Checkpoints.File` + conformance)
- [ ] 9.3b EF Core / S3 checkpointer packages (still deferred)
- [x] 9.4 Microsoft.Extensions.AI integration package (`StemmaGraph.MicrosoftAi` thin helpers)
- [x] 9.5 UI package first cut (`StemmaGraph.UI` + `MapStemmaUI`: inspector / HITL / topology)
