## Context

See proposal.md — Why. Main specs lag shipped post-MVP behavior. This change is
**spec reconciliation only**: implementation already lives on `main` (PRs #17–#23).

## Goals / Non-Goals

**Goals:**
- Delta specs that describe observable host/provider/gate behavior already in code
- Sync deltas into `openspec/specs/*` so validate + archive leave main specs current
- Keep capability set flat (no new capability dirs unless necessary)

**Non-Goals:**
- New runtime features, NuGet tag, PublicAPI internals hygiene (P0 markers)
- Rewriting archive history under `openspec/changes/archive/`
- Full sample migration to Agents.AI

## Decisions

1. **Three modified capabilities only** (`public-api-hosting`, `checkpoint`,
   `quality-engineering`) rather than a new `agents-ai` capability — AI is an
   optional hosting integration boundary, not a second runtime.
2. **ADDED over MODIFIED** for new requirements where existing text stays valid
   (e.g. provider packages, wire version). MODIFIED only where the old
   architecture-tests requirement must gain “already present + CI” language.
3. **Sync then archive** after main specs merge: tasks are file edits + validate,
   not product coding.
4. **Wire version field name** in specs is behavioral (`format version`); concrete
   JSON property name stays an implementation detail already chosen in code
   (`formatVersion`).

## Risks / Trade-offs

- [Spec over-fit to current package names] → Mitigation: requirements use roles
  (file / EF / S3 / agents AI package), scenarios stay black-box.
- [Double-doc drift again after v0.1] → Mitigation: any new public host surface
  lands with a delta change, not README-only.
- [Strict validate fails on scenario format] → Mitigation: follow WHEN/THEN +
  `#### Scenario` only; run `openspec validate` before PR.

## Migration Plan

1. Land delta artifacts in this change.
2. Apply intelligent merge into main specs (sync-specs).
3. Validate main specs.
4. Archive change after merge (or same PR if owner prefers single commit).
5. Proceed to PublicAPI owner OK → `v0.1.0` tag (separate step).
