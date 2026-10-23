# Read-only audit and review-only candidates

Project: `competition-integration-20261020`. Source files were not edited; Unity was not run by this audit. Candidates in this directory have not been compiled or executed. Each `.cs.patch` is independently reviewable; the scope helper must be included with its two callers. No production changes, prices, gates, or assertion weakening are proposed.

## Confirmed shared-state source

`XgSim.YyLearning.cs:38` stores `chatUnlockCatalog` globally. `LoadYyLearning` at line 46 replaces that catalog, and `ChatNodeReady` at line 110 allows every node when the catalog is empty. `XgSim.cs:216` makes that answer a purchase prerequisite.

`XgPersonaExpressionTests.YyTagsChangeLearnedExpressionWithoutMovingTheOpeningWish` at line 84 loads the real Resources catalogs and never restores them. This fixture runs before XgProgressionTests in the full result. Desktop tests can also load the same catalog via `XgYyTalk.Learning.cs:23`; those are additional initialization paths, not necessary to establish this reproducer.

The real `Assets/LingGuangV05/Resources/LingGuangV05/YY/chat_unlocks.json` defines MLP stage 1 phrase `异或怎么分？` and `attention_only` stage 5 phrase `如果只用注意力呢？`; loading maps the latter node to `transformer` (`XgSim.YyLearning.cs:71`). Neither progression fixture had the requisite delivered chat reply. Thus an empty-catalog isolated pass is incomplete test coverage, whereas the loaded-catalog lock is intended runtime behavior. Do not remove the gate.

| XgProgressionTests failure | Cause and minimum correction | Classification |
|---|---|---|
| BuyingTheMlpMovesBothTracksAndLowersAbilityTwo | MLP buy is denied by real chat catalog. Deliver the authored matching reply before purchasing. | Test isolation and stale prerequisites |
| AttentionTrainingLetsTheAiSayItFirstAndOpensTheTransformer | Attention idea succeeds, but Transformer still has an independent chat prerequisite. Deliver the matching reply after the idea before asserting Buyable. | Test isolation and stale prerequisites |
| TheTransformerIdeaAlsoComesWithTimeInStageFive | Same independent Transformer chat gate after the timer succeeds. | Test isolation and stale prerequisites |
| FreshGameReachesTheThirdAbilityThroughPublicActions | MLP never buys, so trained P stays 0.3K despite D=25563. Deliver eligible catalog phrases/replies through RegisterYyReply during the public action loop. | Test isolation and stale prerequisites |

`XgYyCatalogTestScope` snapshots current persona/tag/chat JSON, explicitly loads actual Resources, and restores the previous semantic catalog on Dispose. Both Progression and PersonaExpression candidates use SetUp/TearDown. It also invalidates/rebuilds catalog nodes through the existing LoadYyLearning path. The helper calls public `RegisterYyReply` with the authored resource response, representing the same successful delivered response callback used by the UI; it does not inject `chatUnlocked`. These remain kernel tests, not a network/UI delivery test.

## Missing installed data

`XgSim.cs:205` now requires `S.owned.Contains(dataset) && InstalledBatches(dataset)>0`. `XgSim.DataFiles.cs:27` reads `installedDataBatches` for schema 1 saves; constructor migration marks schema 1 before these fixtures append ownership. `Samples` and `SamplesEffective` therefore remain zero/minimum when only ownership is added. `DatasetAvailable` at `XgSim.cs:445` also prevents non-hand-labelled longtext from being selected. This is intended installation behavior, not evidence that training itself broke.

| Failure(s) | Cause and minimum correction | Classification |
|---|---|---|
| XgChanceHintTests.ALoopOnLongTextIsToldItsStructureDoesNotFit | Owned longtext plus labels still lacks installed entitlement; SetDataset fails at line 91. Arrange installed longtext batch state. | Stale fixture |
| XgDiagnosticsTests.APlainLoopShowsTheDistanceProblem; DiagnosticGroupsComeFromTheCardsRealDistance | OnLongText appends ownership after the constructor migrated schema, then reconstructs an already-current save. DatasetAvailable fails at line 33. Arrange installed longtext before reconstruction. | Stale fixture |
| XgResearchCompletionTests.ReluAlsoMitigatesDeepGradientDecay | Owned MNIST has no installed samples. CapacityFraction clamps at .97 on both sides, masking the existing ReLU multiplier (.85 versus .75 at XgSim.cs:491). Arrange installed MNIST and retain strict improvement assertion. | Stale fixture; numerical candidate still needs execution |
| XingGuangTests.EachPressTrainsOneEpochAndBillsPower | WithPack adds ownership after migration, no installed samples. TrainEpoch returns null. | Stale fixture |
| XingGuangTests.EveryEpochIsAssessedAndOnlyRecordsPay | Same null epoch dereferenced. | Stale fixture |
| XingGuangTests.GradeBonusesArePaidOnce; RecordRewardScalesWithTheDatasetRewardBaseAndPaysOnce | Same unavailable training data yields null assessment. | Stale fixture |
| XingGuangTests.EveryRecordIsSavedToTheRepositoryAndDeployed | No epoch, no model; indexing empty list. | Stale fixture |
| XingGuangTests.LoadingAModelRestoresItsDataAndProgress; RepositoryCleansOldModelsButKeepsStarredAndDeployed | No epoch, SaveModel returns null. | Stale fixture |
| XingGuangTests.AFewDozenPressesBringLeNetCloseToItsCeiling | WithPack has effectively no data; capacity ceiling scores 30 instead of >600. | Stale fixture; keep >600 check and rerun |
| XingGuangTests.OverfittingMakesValidationPeakThenFall; LayersPastTheLimitHurt; TranslationNeedsAnEncoderDecoder | Missing samples clamp curve endpoints, hiding model-dependent effects. | Stale fixture; keep existing numerical comparisons and rerun |
| XingGuangTests.MoreLabelsRaiseTheCeiling | Last phase adds ownership but no installed batch, so pack does not increase samples or ceiling. | Stale fixture |
| XingGuangTests.DataNodesBuyThePack | Purchase is asserted to immediately grant installed ownership. Buy Data Import, download, explicitly install, then assert ownership and training. | Old acquisition expectation |
| XingGuangTests.DesksOpenOnlyWhenTheirDataPackIsBought | Same obsolete immediate unlock expectation for meme/headline/review/cifar/go. Assert closed until installation, then open. | Old acquisition expectation |

The kernel-only fixture candidates declare installed full-pack state explicitly, using the pre-existing full-pack ownership fallback plus an installed-batch count. No production migration is altered. Acquisition tests use actual BuyNode, Tick, DataFileReady, and InstallDataFile calls, with new pre-install negative assertions and retained positive assertions. LegacyStageThree already has LeNet/RNN through XgState defaults (`XgModels.cs:157–160`), so no new architectures or parameter buffs were required in the candidate. If >600 still fails after fixing installed samples, inspect current automatic configuration before changing any thresholds.

## Current training and timer rules

| Failure(s) | Cause and minimum correction | Classification |
|---|---|---|
| XgStageOneTutorTests.AfterTheOpeningTheNoteKeepsToTheMainLineWithItsLesson; EachLessonIsToldOnceAndOnlyInStageOne | Learned helper calls BeginOpening, which sets teachingFlow=true (`XgSim.Opening.cs:83`). TeachingTrainOpen requires train.curve (`XgSim.TeachingFlow.cs:14`). Helper never buys it, so all 40 training attempts return null and opening persists. Follow existing passing XgOpeningTests.PlayUntilLearned: buy train.curve when TeachingTreeOpen. | Stale fixture |
| XingGuangTests.ComboIsSharedTimesOutAndHasTiers | Idle no longer breaks the player streak. Require preservation across Tick and training, preserve tier checks, and verify an actual wrong answer still breaks it. | Intentional unlimited-combo rule, old assertion |
| XingGuangTests.GoldCardsPayTripleButExpire | TickCard now only increases age and retains the question (`XgSim.cs:1664`); Answer at 1428 removes the triple bonus when late without timing out or breaking the streak. Verify retained card ID, no timeout, preserved streak, ordinary late-answer payment, no gold bonus. | Intentional timer rule, old assertion |

## Validation limits and next check

All root-cause chains above are established by source and the full-suite XML, not by new execution. The four progression failures have a particularly exact static-initialization explanation, but parent-controlled isolated/full tests should establish the final result. Some data-dependent numeric tests can reveal additional balance assumptions only after installation is fixed; candidate assertions deliberately retain their original thresholds. No production bug is demonstrated by these failures as currently observed.

Suggested focused run: all eight candidate fixtures together (including PersonaExpression), followed by the parent-owned full suite. Also check Progression alone with actual catalog loaded; it should behave identically. No build/package/release operation is needed.
