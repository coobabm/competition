# Brain cards — Core validation

## Scope

Only these source files were added (plus Unity .meta files):
- `/Users/kk/competition/Assets/LingGuangV05/XingGuang/Core/XgBrainCards.cs`
- `/Users/kk/competition/Assets/LingGuangV05/XingGuang/Tests/Editor/XgBrainCardTests.cs`

No existing simulation source or tests were edited by this task. No Unity Editor control or Play-mode validation was performed by this subtask.

## Results

- TDD RED: 26 failed / 26 total against an empty adapter, with assertion failures after correcting the initial test-fixture float literal compile error. `core-red.log`.
- Focused GREEN: 26 passed / 26 total. `core-focused.log`.
- Full existing .NET suite: 593 passed, 29 failed, 0 skipped, 622 total; duration 7m19s. `core-suite.log`.
- Baseline confirmation: a temporary copy of the existing test project used identical absolute source includes, explicitly removing both new card source and test files. All 27 selected inexpensive failures reproduced without this change; 27 failed / 27 total, 18s. `core-baseline-failures.log`, `core-baseline-failures.trx`, and `baseline-failing-filter.txt`.
- Two remaining full-suite failures were NOT baseline-retested: `EachStagesWallPassesWithItsGoldenSetting(2)` and `FreshGameCanReachAllSixStagesThroughPublicActions`. Their baseline status remains unverified; the latter took 4m14s in the full run.

Commands:
```sh
dotnet test Tools/XingGuang/tests/Tests.csproj --filter FullyQualifiedName~XgBrainCardTests --no-restore --verbosity minimal
dotnet test Tools/XingGuang/tests/Tests.csproj --no-restore --verbosity minimal
```

## Behavior and complexity

- Cards modify `XgTrialDraft.proposal` only; existing `IsTrialCurrent` gates edits, and existing `TryApplyTrial` remains the only apply path.
- Required architecture / activation cards cannot be removed. ResNet / Transformer display fixed built-in skip links. Attention-only requires the attention architecture and is removed when that architecture is replaced.
- An architecture apply follows existing `Restart`: resets `steps` (or retains 60% with transfer), clears training histories, reevaluates. It does not itself erase the concept board. Existing depth-change behavior is unchanged.
- `Available`: linear catalog / ownership lookups, no draft fingerprint allocation. `Place` / `Remove`: existing fingerprint cost O(F) time and temporary space for F serialized model-state bytes, plus small catalog lookups. `HasChanges`: O(1) time and space.

## Full-suite failures
- TinyRateIsSlowAndHugeRateTears [18 ms]
- StepActivationOnTwoLayersIsNamed [56 ms]
- ThePlaytestLeNetThatStaysAtChanceIsToldToWiden [107 ms]
- AMetContractSendsBackLogsSlowlyBeforeStageFive [1 ms]
- TheModelLabelsLogsAsWellAsItIsRight [13 ms]
- APlainLoopShowsTheDistanceProblem [1 s]
- PassingIsJudgedByBehaviourNotByTheReferenceSetting [1 s]
- TheExamIsIndependentFarAndUnseen [1 s]
- TrialsAreFairAndChangeNothingReal [1 s]
- PretrainingStallsWithoutScaleAndTripsWithoutAServerRoom [41 ms]
- StageFiveReadsTheGarbleLineByLine [12 ms]
- AfterLabelsItSuggestsTrainingThenAnAssessment [27 ms]
- GlobalAutoKeepsEachDesksCheckpointAndSamplesSeparate [27 ms]
- LogicDeskGetsHarderAndPaysMore [13 ms]
- DriftLowersCheckpointAccuracyByFifteenPointsForAutoAndGhost [13 ms]
- TheScaleNamesTheNearestFamousModel [< 1 ms]
- VanishingMemorisationAndAmnesiaAreDetected [51 ms]
- EachStagesWallPassesWithItsGoldenSetting(2) [14 s]
- FreshGameCanReachAllSixStagesThroughPublicActions [4 m 14 s]
- LongSentenceWallIsReliableAcrossSeedsAndHistoriesAndIsNotALock [1 s]
- StageTwoNeedsBothDesksEachWithItsOwnWiring [8 s]
- LevelFourStopsAutomaticTrainingAfterThreeStaleAssessments [19 ms]
- AutoAnsweringNeedsACheckpointAndPaysOnlyRightAnswers [115 ms]
- AutomationTrainsOnItsOwn [14 ms]
- ContractsNeedTheThresholdAndPayIntoTheWallet [13 ms]
- EachPressTrainsOneEpochAndBillsPower [20 ms]
- EveryFourthEpochIsAssessedAndOnlyRecordsPay [18 ms]
- SuggestionIsRightAsOftenAsTheCheckpoint [8 ms]
- VramLimitsModelSize [31 ms]
