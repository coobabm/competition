# C1 rare relic expansion — independent reviewed candidate

Prepared against HEAD `bf354fdbce3745a81056fec6d17452ca91b15f21` and the current uncommitted life-event work. No production files were edited, no Unity process launched, no commit/push performed. This is a candidate, not a playable release claim.

## Integration artifacts

- `relic-expansion.patch`: full 12-file patch: eight production files plus two Editor checks and their metas. Apply from the production project root after review.
- Alternatively apply `shared.patch` + `relic-core.patch` + `ui.patch` + `checks.patch`. These contain exactly the same changes; do not also apply the full patch.
- `shared.patch` contains only the C1 hunks in TrainingCatalog, Progression and TechTreeCopy so the parent can serialize shared-file integration with other modules.
- `base/`: current eight-file baseline. `candidate/`: complete candidate files for review, not for blanket copying over later changes.
- `manifest.json`: baseline and candidate hashes. `static-verification.json`: patch application check and production baseline comparison.

The old candidate's TechTreeCopy was behind current production: the shortened GRU wording and Cross-sentence English label are preserved. All other seven old baseline files matched the current production baseline. Old generator scripts were not rerun.

## Requirements and review findings

- New/legacy games retain four slots. Capacity comes from purchased node IDs, never from saved slot-list length.
- `relic.slots8`: stage 3, ordinary price 12,000. `relic.slots12`: stage 4, ordinary price 48,000, requires the first. Both remain optional.
- Catalog avoids StagePrice; NodeCost avoids the legacy 0.25 multiplier. Actual BuyNode remains the sole purchase path with normal affordability, duplicate, parent and stage checks.
- Existing legal relic discounts are retained, including stacked Coupon + Angel term sheet. DiagnosisFactor stays in the same quote path. Existing params/structure diagnosis coupons do not apply to Research nodes and are not consumed; their eligibility has not been widened.
- All 12 positions participate in effect cache invalidation, equipment operations, set bonuses and serialization. Normal old four-slot inventory/positions remain unchanged. Invalid extra equipped positions are trimmed without removing those items from owned inventory. Existing malformed-save ownership/uniqueness repair behavior remains intact.
- Backpack displays all 12 slots across three scrolling rows, dynamic capacity and locked-slot tech routing. Top bar retains its 143-pixel budget and uses four slots per page across three pages. Full equipment chooser has 12 targets.
- Additional issue found and fixed: replacement chooser could survive a lab/save rebind and operate against the new Sim. The chooser now binds to its original Sim, closes on page entry/rebind, rejects stale callbacks, and clears old per-visit inventory state on rebind. Top-strip page resets on Sim change. Fresh-item marks are preserved when entering the page.

## Executed verification

`dotnet run --project Checks.csproj --verbosity quiet`:

- 1,253 new assertions passed against actual current Core sources with the five candidate Core replacements.
- All 49 existing `XgRelicTests` passed unchanged.
- Tests cover real purchases/debits, insufficient funds, gates/prerequisite, duplicates, original slot preservation, 8/12 roundtrip, every new slot, all 24 relics in slot 12, all six cross-page pairs, full inventory replacement/movement, repeated repair, legal stacked discounts, unrelated diagnosis coupons, tech copy/addressability.
- `core-result.json` contains the result and individual legacy outcomes.
- The .NET harness uses System.Text.Json for serialization; its tiny JsonUtility substitute is harness-only and is not included in the production patch. This is not Unity JsonUtility or disk-save validation.
- Compiler warnings were pre-existing DayStats CS1718 and unused Sim/Quality events CS0067.
- `git apply --check relic-expansion.patch` passed; all eight production baseline files were unchanged at finalization.

## Parent's remaining serial Unity validation

1. Import/compile the reviewed patch using the parent's single Unity session.
2. Execute `RelicExpansionChecks.Run` (menu Tools/Local Verification/Relic expansion purchase and persistence). It runs the same 1,253 assertions with actual Unity JsonUtility.
3. Execute existing `XgRelicTests` via the Unity Test Runner, including corrupted/legacy save repair.
4. Embed `RelicExpansionUiChecks.Frames(ctl,setEnglish,click,capture)` in the existing isolated SidebarLayoutChecks real-economy fixture (`useDiskSave=false`, `tickEnabled=false`, `TestMode=false`). Use live LCD pointer/capture callbacks and render each yielded frame.
5. UI checks cover Chinese/English × 960×600/1400×900, all top-strip page navigation and locked targets, actual host debits, third-row access and slot-12 unequip, full twelve-button chooser/replacement, and save-rebind stale callback protection. These checks were authored but **not executed** here. Inspect captured images for clipped typography, the third row and chooser hit-target visibility; test real scrolling/input.
6. Confirm real old disk save load/save and expanded disk-save roundtrip without item loss. In-memory JSON checks do not substitute for that.

No new product ambiguity identified. No changes outside C1 are proposed.
