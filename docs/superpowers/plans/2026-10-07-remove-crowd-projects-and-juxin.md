# Remove retired crowd projects and Juxin

> **For agentic workers:** Use subagent-driven-development; preserve all unrelated working-tree changes.

**Goal:** Execute the user's deletion request: retire the six reviewed crowd items and remove the in-game Juxin app.
**Architecture:** Delete playable offers and application behavior, not just their visible buttons. Keep main-story training capabilities and narrowly scoped legacy-save compatibility. Reuse existing catalog, repair, and desktop initialization paths.
**Tech Stack:** Unity 6000.4.5f1, C#, existing NUnit/.NET 8 Core harness.
**Spec:** User request in this chat: “把这些删掉 删除巨信这个软件”.

## Constraints and review focus
- No unrelated rollback, dependency installation, scene YAML edits, or commits.
- Remove normal contracts `goclub`, `parking`, `support`; remove crowd desks `translate`, `crosssentence`; remove every SLA offer and its execution/UI.
- Preserve translation/cross-sentence training and stage progression; do not unlock late datasets at new game.
- Retired saved desks, contracts, worker assignments and wiring must not retain runnable work or income.
- Existing wallet balances and acquired training data remain; previously earned SLA escrow is paid once, never repeatedly.
- Juxin has both explicit and automatic startup paths. Remove both, its background replies, and its wiring node; its reused Photo Gallery slot must not become an unexpected replacement app.

## Task 1: Crowd catalog and saved-state migration (worker)
- [x] Add and run failing regression tests for removed contracts/desks and preserved teaching/progression.
- [x] Edit `XgCatalog.cs`, responsible `XgSim` repair/access paths, story bridge and finale source text only as needed.
- [x] Run focused NUnit tests; report any retained legacy-only API.

## Task 2: Juxin application removal (worker)
- [x] Replace Juxin-presence tests with retirement tests, run red first.
- [x] Delete Hub/View and core business logic, remove startup/QA/wiring dependencies, hide the reclaimed native slot using existing desktop patterns.
- [x] Run focused tests for safe old-save loading and absence of Juxin wiring/background work.

## Task 3: SLA removal and integration (root)
- [x] Add failing tests asserting `typeof(XgSim).GetMethod("SignSla") == null`, legacy escrow settles once, invalid escrow is discarded.
- [x] Remove SLA models, offer catalog, timers, events and UI; keep monthly meme drift and subcontracting intact.
- [x] Retain only `XgState.slaHeld` as legacy escrow and clear it after the first valid tick payment.
- [x] Remove SLA/Juxin ledger, credit and localization residue; update documentation to mark old design sections historical.

## Task 4: Verification
- [x] Compare against the pre-change snapshot in `/tmp/lingguang-remove-crowd-juxin.HhGYTm`.
- [x] Run `dotnet test Tools/XingGuang/tests/Tests.csproj --no-restore` and report baseline/remaining failures by name.
- [x] Refresh Unity, inspect compilation/Console, run focused EditMode tests and bounded runtime checks without altering the user's saved progress.
- [x] Independent review of the actual diff; fix only task-related defects and state remaining verification limits.

## Verification outcome

34/34 removal regressions, 610/610 non-baseline regressions, 6/6 direct Unity editor fixture methods, and real Main preview-scene retirement assertions passed. Original 29 Core failures were reproduced against the pre-change snapshot, not repaired. Full Play-through was not performed. Detailed evidence: `Documentation/LingGuang-Incremental/QA/content-removal-2026-10-07/README.md`.
