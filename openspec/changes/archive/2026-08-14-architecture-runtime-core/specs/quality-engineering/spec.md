## Purpose

Defines how Voluta is verified and released: unit scenario coverage for the runtime core, checkpoint conformance binding, a benchmarks project, and CI gates including pack smoke and OpenSpec validation.

## ADDED Requirements

### Requirement: Unit scenario matrix for runtime core
The repository MUST maintain automated unit tests that cover at least: LastValue multi-write rejection; Append multi-write merge with stable order; single-node linear graph; two-ready same superstep; barrier visibility (intra-step writes hidden); recursion limit; interrupt + resume; resume when not interrupted fails; thread isolation for two thread ids; stream cancel stops further supersteps.

#### Scenario: CI runs unit matrix
- **WHEN** CI executes non-integration tests on a PR that touches runtime
- **THEN** the matrix scenarios above are included in the test run and failures fail the gate

### Requirement: Conformance bound to InMemory in CI
The checkpoint conformance suite MUST run against the InMemory provider on every CI unit test pass for the solution.

#### Scenario: Conformance in default test filter
- **WHEN** `dotnet test` runs with the default CI filter (excluding Integration)
- **THEN** InMemory conformance tests execute and pass

### Requirement: Benchmarks project exists
The repository MUST include a BenchmarkDotNet (or equivalent) benchmarks project covering at least: empty/superstep overhead, cyclic single-writer path, parallel ready + Append merge, and InMemory checkpoint put/get for a representative state size.

#### Scenario: Local bench runnable
- **WHEN** a developer runs the benchmarks project locally
- **THEN** the listed benchmarks execute without requiring external services

### Requirement: Benchmarks do not fail PR by default
CI MUST NOT require benchmark regression failure on every PR in MVP unless an explicit baseline gate is later enabled. Benchmarks MAY run on schedule, manual dispatch, or non-gating job.

#### Scenario: PR green without bench job
- **WHEN** a PR only changes docs or non-hot-path code
- **THEN** absence of a gating benchmark job does not block merge

### Requirement: CI build and unit test gate
Every PR to main that changes product code MUST run restore, build with warnings-as-errors, and unit tests (non-integration) on ubuntu-latest with the solution’s target framework.

#### Scenario: Analyzer warning fails CI
- **WHEN** a PR introduces a compiler/analyzer warning treated as error
- **THEN** the CI build job fails

### Requirement: CI pack smoke
CI MUST pack packable library projects (or dry-run pack) without pushing, so packaging breaks are caught before a release tag.

#### Scenario: Pack on PR
- **WHEN** CI runs on a PR that changes src library projects
- **THEN** a pack step produces nupkg artifacts (or fails the job on pack error) without publishing to nuget.org

### Requirement: OpenSpec validate on planning changes
When OpenSpec change or main spec files are present in the PR, CI MUST run `openspec validate` (or equivalent) so invalid specs cannot merge unnoticed.

#### Scenario: Broken spec header
- **WHEN** a PR introduces a spec requirement without a scenario block
- **THEN** the OpenSpec validation job fails

### Requirement: Testing package packability policy
`Voluta.Testing` MUST either be non-packable until intentionally published, or be excluded from the default release pack set, so accidental nuget.org publish of test helpers does not occur.

#### Scenario: Release pack set
- **WHEN** the release workflow packs shippable packages for a version tag
- **THEN** Testing is not pushed unless the release explicitly includes it

### Requirement: Architecture dependency tests when layers exist
Once multiple library projects exist, the repo MUST include architecture tests that `Voluta.Abstractions` has no forbidden dependencies and that core does not reference EF/S3 provider packages.

#### Scenario: Core must not reference EF package
- **WHEN** architecture tests run
- **THEN** a ProjectReference from core runtime to an EF checkpointer package fails the test
