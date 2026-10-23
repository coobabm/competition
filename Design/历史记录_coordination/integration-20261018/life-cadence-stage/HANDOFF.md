# Life cadence candidate

Production is unchanged. Do not run `prepare.py` after integration: it captures the current production baseline before creating candidates.

Apply `life-cadence.patch` together with the separate host candidate. Only these production files change:

- `XgSim.LifeEvents.cs`: versioned per-save cadence, runtime gates, result snapshots, acknowledgement, draw pool cycling.
- `XgSim.LifeData.cs`: removes stage resets and two-event cap; captures bilingual results from the one actual commit.
- `XgLifeEventCard.cs`: choice/result presentations and a bilingual confirmation button.

Interfaces (agreed with the host agent):

- `LifeEventActive` defaults true, runtime only. Host pause/native game minimization sets false. Internal app visibility is not a clock gate.
- `LifeEventConflict` runtime only, ORed with existing presentation holds. Positive remaining time freezes. An already-due draw records a deferred flag; after conflict and inactivity both end, a full 30-second active grace starts. Host calls `ObserveLifeEventConflict()` before its existing stage-presentation skip-Tick branch; this method advances neither clock.
- `LifeEventIntervalMode`: 0=480 seconds, 1=900 seconds, 2=off. Serialized as `lifeCadenceMode`. A changed value resets a full chosen interval, assigning the same value does not. Off preserves existing choice/result cards.
- `LifeEventResultPending`, `LifeEventResultView()`, `AcknowledgeLifeEventResult()`: the result is a persisted snapshot of the committed outcome in both languages; acknowledgement clears it and restarts cadence once. Viewing/reloading never invokes effects. The card calls existing `controller.SaveNow()` after choice and acknowledgement; existing save failure status remains authoritative. It validates both the bound lab and bound house simulation before accepting stale UI callbacks.

Migration has its own `lifeCadenceVersion`. Old random countdowns migrate to 480 seconds/default enabled without reseeding a nonzero RNG or erasing permanent seen/history/flags/returns. Stage changes do not reset cadence. `lifeClock` continues independently for repayments, temporary card holds, training pauses and boosts.

Approved cycle A: build the currently eligible pool using unchanged stage/date/requires/when/cooldown checks; globally unseen eligible rows win first, then rows not yet in the current cycle. Once all currently eligible rows have appeared, the next open clears only those eligible rows from the cycle set. Permanent seen, history and effects are retained. Data JSON is untouched.

Verification:

- `dotnet run --project Checks.csproj --verbosity quiet` passed 127 real-simulation and wallet checks, including real BeginOpening + twelve answers + curve purchase while openingFocus remains true, and stage conflicts observed without Tick. `core-result.json` records the result. Existing unrelated compile warnings: CS1718 in DayStats, unused events in Quality/Sim.
- Earlier fractional 0.1-second boundary assertion exposed floating-point residue; the candidate now uses 1e-9 deadline tolerance and the same test passes. No old test was weakened or rewritten.
- Unity has not been started. Copy `LifeCadenceChecks.cs` and `LifeCadenceEditorChecks.cs` into an Editor-check location, then invoke `LifeCadenceEditorChecks.Run` to repeat with Unity JsonUtility.
- Result-card visual/input checks and host integration remain for the coordinator. `git apply --check` passes at the captured baseline.

## Legacy-check candidates

`legacy-checks-combined.patch` is a separate test-only patch for `XgLifeEventsTests.cs` and `WorldCompletenessQa.cs`. Alternatively apply `legacy-cadence-checks.patch` then `legacy-data-fixtures.patch`; do not apply both alternatives.

- Timing expectations now use train.curve ownership, 480 seconds after acknowledgement, frozen holds and a 30-second deferred grace. World QA now expects a third event, replacing the obsolete stage cap. Every successful legacy choice test explicitly acknowledges its result before continuing.
- Old download tests incorrectly treated copied data as already installed. They now check ready/not installed, purchase Data Import through its real node, install the file, verify no additional charge, and reject duplicate installation. Full-pack size comes from the current catalog.
- Old bad-sector fixtures appended `owned` after repair but never supplied installed batches. They now invoke the actual old-data migration and keep all original lost-batch/sample/fee assertions.
- Recurring broadband reminder checks first handle fresh eligible nudges using their free choices, then verify the repeat reminder and unchanged renewal/monthly billing assertions.
- The stage-four forum invitation now asserts its actual neutral invitation and absence of premature praise; post-choice achievement and result consequences remain checked.

Standalone execution of the original 25 tests against the original core had eight failures (`baseline-legacy-result.json`). After the explicit expectation/fixture updates, all 25 pass against the candidate (`legacy-result.json`). This is .NET reflection execution of real NUnit assertions, not a Unity Test Runner claim. World QA still awaits Unity. No production tests were edited.
