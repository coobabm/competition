# Training signature cache reuse — staged review fix

`reuse-training-signatures.patch` touches only XgBoardData, XgTestSetPreparation, and TestSetPreparationChecks. It is based on production after the finer profiling markers were added and preserves those markers. No production source was changed by this agent, and no Unity process was launched.

The new internal TryGet method looks up the existing exact dataset/level/FULL date/salt key. It returns a membership delegate to the completed immutable-by-convention set, exposing no mutation API. A matching builder borrows that lookup, skips generating 4096 training cards, and releases the delegate on completion. Missing entries still use private incremental construction. Nothing is published to the shared cache early. TrainingGenerated now reports actual generated cards (zero on reuse), with ReusedTrainingSignatures identifying that case.

The added check uses actual XgBoardData.TestSet both before and after changing an InvariantCulture clone's NegativeSign to '~'. Its CultureInfo.Name remains unchanged. It compares ordered card content, verifies cache reuse, and restores culture in finally. This addresses the existing reference-only test's blind spot: literal reconstruction does not reflect the real static TrainSignatures cache.

Validation completed:

- Ordinary git apply --check against the current production working tree: passed.
- Separate staged Roslyn compilation of LingGuangV05.XingGuang.Core and Assembly-CSharp-Editor: exit 0 for both. Outputs and logs stay in this staging directory.
- Independent PowerShell/.NET process loading the original compiled assembly reproduced 400/400 different ordered cards for mnist/level1/date0/salt7593 after the culture format change. Expected first seed 331770666; original builder first seed 331980124.
- Independent process loading the staged compiled assembly produced 0/400 different ordered seed/signature pairs, complete=true, reused=true, TrainingGenerated=0; both first seeds 331770666.
- Full Unity EditMode checks with the staged changes have NOT been run; root owns that serial integration step.

## Cold Vocabulary issue (separate from this patch)

XgBoardData.Make -> FromCard -> Words -> Vocabulary initializes a dataset with 4000 XgMemes.Pick calls and bigram aggregation inside ONE Advance(1). The 1 ms scheduler check occurs afterward. Independent cold .NET-process timings (not Unity frame acceptance): spam first unit 39.31 ms / second 0.21 ms; headline 5.22 / 0.03 ms; review 20.63 / 0.04 ms. Reference() runs first in the existing suite, masking the cold Vocabulary path.

Smallest containment for the current targeted rollout: have the scheduler skip a Words dataset while its vocabulary is absent, using an internal read-only readiness probe; resume preparation after normal gameplay has built that vocabulary. This prevents moving an additional cold vocabulary stall into idle preparation, but does not remove the existing synchronous vocabulary stall. Stable-context logic preparation does not enter Words.

Complete treatment: make vocabulary initialization a separate incremental prerequisite, preserving dataset.GetHashCode random seed, all 4000 picks, phrase deduplication, bigram counting, threshold >=2, and atomic complete-only publication. No lock may span a frame. This is a distinct change and should not be hidden inside this narrow cache patch.

Cold verification should run in a fresh verification process/domain for each vocabulary dataset, before any Reference/Make/TrainEpoch; measure every Advance(1), allocations, and the idle marker. Warm verification should then repeat with populated vocabulary. Include both successful prepared consumption and an early synchronous request that discards partial work.

## Scope and remaining limits

Full pending date matching is correct: an unconsumed October 18 set is rejected on October 19, while an already-live same-month set retains the original reference. Salt and level matching are correct. English is only presentation here; Make does not read sim.English. The English guard is conservative and costs an occasional restart.

The scheduler remains opportunistic with one pending set. A cached/ready active vision track can starve another active track; ObservePhenomena may also request old same-region datasets for forgetting. Those remain documented coverage limits, not changes in this patch.

This patch reuses a cache entry that exists when the builder starts. It does not generalize to externally mutated catalogs, a new competing static-cache entry appearing mid-construction with a different formatting history, or mutation of CurrentCulture.NumberFormat without a Name change during a pending job. The original Name-only pending guard cannot detect that last case. Do not claim universal equivalence across arbitrary mutable generation environments.
