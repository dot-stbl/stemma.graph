## 1. Delta artifacts

- [x] 1.1 Write `proposal.md` (why/what/capabilities)
- [x] 1.2 Write delta specs for public-api-hosting, checkpoint, quality-engineering
- [x] 1.3 Write `design.md` (sync-only approach)
- [x] 1.4 Write `tasks.md` (this file)

## 2. Sync to main specs

- [x] 2.1 Merge `public-api-hosting` ADDED requirements into `openspec/specs/public-api-hosting/spec.md`
- [x] 2.2 Merge `checkpoint` ADDED requirements into `openspec/specs/checkpoint/spec.md`
- [x] 2.3 Merge `quality-engineering` MODIFIED + ADDED into `openspec/specs/quality-engineering/spec.md`
- [x] 2.4 Run `openspec validate --specs` (and change validate) — fix any format issues

## 3. Ship planning hygiene

- [x] 3.1 Update `.agents/roadmap.md` — mark OpenSpec sync done / remove gap note
- [x] 3.2 Commit + PR (or archive change after sync per owner preference)
- [x] 3.3 Archive `sync-post-mvp-specs` (manual move — `openspec archive` aborted because main already had ADDED headers after agent sync)

## 4. After this change (out of band)

- [ ] 4.1 Owner OK on PublicAPI review → optional P0/P1 internals
- [ ] 4.2 Tag `v0.1.0` + nuget.org publish + Unshipped → Shipped
