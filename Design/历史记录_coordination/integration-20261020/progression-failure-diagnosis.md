# EditMode progression failures — isolated diagnosis 2026-10-20

Scope: XgProgressionTests three failures from editmode-tests.xml. No Unity process launched; no commit. Formal source edits frozen after checks.

## Evidence and resolution

1. FreshGameReachesTheThirdAbilityThroughPublicActions was stale after the explicit data-install feature. Its loop bought/downloaded packs but never installed one. Baseline pure-core execution reproduced exactly stage=1, P=8.2K, D=3849. P already exceeded ability 2's 6K * MLP .8 = 4.8K, but D stayed below 6000. Added actual InstallDataFile calls for ready files only after Tick, only when CanInstallData. Kept 1800 seconds, expected stage 3, emergence {1,2}, and firstTrack assertions. It now passes.

2. FreshLabStartsAsAPerceptronOnLogicAndSpamWithTheSeedPlanted asserted arithmetic closed until stage 2. User-approved current basic-arithmetic stage-1 design makes that obsolete. Changed assertion to open, explaining advanced arithmetic still uses purchased upgrades. No production gate changed.

3. LayoutHasNoOverlappingNodesAndNoFutureColumnsAtStart first failed inside XgCatalog.Validate because it incorrectly required every Research-kind node to exist in the model-effect Research catalog. Feature unlocks (train.curve, brain.window, data import), teaching packs, personality packs, daily abilities and board upgrades legitimately use Research kind without a model-effect entry. Validator now walks every registered model research effect and requires a node with identical id, Research kind and exact target id. It keeps duplicate, parent, dependency, architecture/dataset, cycle, catalog completeness and desk validation. Added a negative regression that corrupts a real model-research target, requires the validation error, restores it in finally and revalidates.

After Validate was fixed, the layout test exposed its second stale premise: NodeVisible deliberately lets the sealed persona archive and two relic expansion technologies be located before purchase. The test now permits exactly those linked entries, requires all future entries to remain Locked, requires attempted purchase to fail without charge, and continues to require every other future node hidden. All geometry overlap assertions unchanged.

## Modified formal files
- Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs
- Assets/LingGuangV05/XingGuang/Tests/Editor/XgProgressionTests.cs

## Validation
A temporary net10.0 console harness outside Assets compiled the actual current Core + XingGuang/Core sources and actual XgProgressionTests against the installed Unity NUnit assembly. Baseline reproduced all three original failures. Final run returned exit code 0:
PASS FreshGameReachesTheThirdAbilityThroughPublicActions
PASS FreshLabStartsAsAPerceptronOnLogicAndSpamWithTheSeedPlanted
PASS LayoutHasNoOverlappingNodesAndNoFutureColumnsAtStart
PASS CatalogStillRejectsAnInvalidModelResearchTarget

Targeted git diff --check passed. Unity EditMode confirmation remains with the parent task. Harness is in progression-diagnostic/Diagnostic.csproj next to this report. Existing unrelated compiler warning XgSim.DayStats.cs:33 compares a value with itself; no changes made there.
