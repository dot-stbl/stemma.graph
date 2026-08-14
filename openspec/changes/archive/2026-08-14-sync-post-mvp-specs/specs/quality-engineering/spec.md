## MODIFIED Requirements

### Requirement: Architecture dependency tests when layers exist
Once multiple library projects exist, the repository MUST include architecture tests that `Voluta.Abstractions` has no forbidden dependencies and that core does not reference EF/S3/File provider packages, UI, or Agents.AI packages. Those tests MUST run as part of the default unit test gate.

#### Scenario: Core must not reference EF package
- **WHEN** architecture tests run
- **THEN** a ProjectReference from core runtime to an EF checkpointer package fails the test

#### Scenario: Architecture tests in CI unit filter
- **WHEN** CI runs non-integration unit tests for the solution
- **THEN** the architecture package isolation tests execute and pass

## ADDED Requirements

### Requirement: PublicAPI ship gate on packable libraries
Packable product libraries MUST track public surface with PublicAPI shipped/unshipped baselines so accidental public API additions fail the build before a release tag.

#### Scenario: Undeclared public symbol fails build
- **WHEN** a packable library adds a public type or member not listed in the PublicAPI baseline files
- **THEN** the solution build fails with a PublicAPI analyzer error

#### Scenario: First NuGet tag freezes surface
- **WHEN** the first NuGet version is published
- **THEN** the release process moves unshipped surface into shipped and further public changes must land as unshipped deltas (or intentional breaking review)
