# C5 isolated arithmetic candidate

Baseline: `2183260a`. Production has not been edited. No Unity, player, production model training, or git commit has been run by this subagent. After parent authorization, the actual candidate core was compiled and executed in the isolated .NET 10 harness.

## Delivered

- Stage 1 free: addition, subtraction (including negative results), multiplication, nonzero division with exact fractions, parentheses, operator precedence.
- `arith.powers`: stage 2, ¥800. Negative-base powers, leading-minus precedence, real principal square roots, √(x²).
- `arith.calculus`: stage 3, ¥3,000, parent `arith.powers`. Two-sided removable-singularity limits, polynomial derivatives at a stated point, definite polynomial integrals, indefinite polynomial integral families with arbitrary C.
- `arith.auto`: stage 2, ¥1,500, no invented `label.ai` or powers prerequisite. Only `arith_mole` and its legacy `dadishu` implementation. These prices are appended directly to the ordinary catalog, outside `StagePrice`; the minimal `XgSim.Progression.cs` hunk also bypasses `StoryResearchPrice=.25` while retaining existing discounts.
- Generated claims use exact reduced rationals. False numeric claims differ by exactly one; no ambiguous free-form algebra answers, denominator zero, non-real roots, multiple-solution equations, missing C, or unspecified one-sided limits enter the generated pool.
- `XgArith.Generate`, `XgBoardData`, training code, training pools, and TestSet files are untouched. The new game generator is intentionally separate from training content; it does not introduce a second settlement system.
- Both manual and automatic whacks go through the same guarded `Hit` → `C.Report` → existing host `SubmitMiniGame` → `SettleMiniAction`. No direct wallet/sample mutation, new speed reward, or golden-mole cash bonus. Golden moles retain the prior extra game-score rule.
- A real model readout selects whack/leave. No skill/truth RNG, no answer correction, and no fake confidence percentage. A cold readout is marked “guess”. No new inference billing rule is invented.
- Leave decisions do **not** pay: the existing game pays only actual whacks. A false AI leave of a target naturally becomes an existing missed mole. A player may still hit a mole after the AI chose leave.

## Label-blind inference and remaining growth limitation

`XgMoleInference.Input` reads only displayed `question` and `category`. Token features and the stable seed come from those strings. It does not read `truth`, `Expected`, the correct answer, level, or generation seed. `PredictMole` invokes the existing live `Board.Predict` with current arithmetic-track knobs.

The old `XgBoardData.Logic` input encoder (production lines ~466–501) derives “看着对/验算成立” clues from the answer label. Reusing it would leak answers, so this candidate does **not** call it. The existing `TeachingGuess`, `TeachBoard`, and correction/retest paths still use that encoder; none is a suitable label-blind arithmetic entry point.

**C5's model-growth acceptance criterion is not complete.** Existing training can change a shared `类:<category>` vote, but the new positional `算符:<token>` vocabulary is not emitted by the existing arithmetic training encoder. Consequently there is no demonstrated path for the current training pool to teach the newly exposed symbol relations. The actual auto-action works and can fail, but reliable arithmetic improvement must not be claimed. The candidate contains a test proving live-weight dependence, explicitly not an efficacy test. A later approved input/training-data adapter decision is required; this subtask was expressly forbidden from changing training/model/TestSet to make the AI succeed.

## Feedback contract for parent-owned presentation

Subscribe to `sim.ArithmeticMoleFeedback` and unsubscribe with the surface owner. `XgMoleFeedback` is immutable and contains:

| Member | Meaning |
|---|---|
| GameId / Slot / QuestionToken | Actual game, 0-based hole, sim-lifetime monotonic question identity |
| Automatic / Whack | Action source and whack versus explicit AI leave |
| Correct | Whether that chosen action agrees with the displayed claim and this game's target rule |
| Reported | The whack called the existing report path; **not** a new payment receipt. Actual payout remains the host's existing `Answered(XgMiniResult)` event |
| Question | Full displayed claim, exact expected/shown value, kind, bilingual explanations/conditions |
| Prediction | Actual `XgPredictionTrace` snapshot; can be null for unaided manual whacks |

The callback is notification only. It must not submit another answer. A whack consumes `XgMoleTurn` before notebook/report/event calls; reentrant callbacks, two clicks, auto-after-manual, previous question tokens, rebuild generations, expired turns, closed/disposed contexts, and inactive surfaces cannot resettle that turn. There are no delayed coroutines/tasks. Tick uses only active dt, rejects non-finite dt, expires a mole before an overdue AI action, and aborts after callback-driven hide/dispose/rebuild. Non-active frames do not advance the AI delay.

The old 132×28 sign is inadequate for calculus. **Parent must finish larger/expanded question presentation before release** so the player can read the whole claim and conditions. Animation, sounds, reduced effects, preview/popup presentation, and runtime click-through checks belong to the parent. The added auto button is minimal plumbing and can be restyled. The transient toggle defaults to on for an owned tech whenever a new round builds; no persistence rule was requested.

## Verification and integration

- `static-report.json`: 3,284 passing static/reference assertions, including 2,688 exact mathematical parameter cases. This is a Python reference audit, **not execution of the C# generator**.
- New `XgMoleArithmeticTests.cs`: independent parser/numerical checks of **displayed** generated claims, all tier coverage, fraction equivalence, truth/seed mutation invariance, cold wrong predictions, inference purity, live model coupling, token invalidation, reentrant payment, prices/prerequisite tests. **All nine executed and passed in the .NET harness.**
- `git apply --check --ignore-space-change --whitespace=nowarn production.patch` passed against production baseline. `manifest.json` confirms all candidate baselines remain unchanged in production.
- Ten production artifact paths: four modified sources, three new sources, three new metas. The fourth modified source is the one-line ordinary-price exception in `XgSim.Progression.cs`. The third modified source updates one obsolete existing test from stage-two arithmetic to the approved stage-one free opening. `XgActionGames.cs` has only the `XgActionMoles` class changed. `XgTrainingCatalog.cs` has only the three node insertions; do not overwrite a parent's concurrent catalog edits.
- Parent should apply the localized patch, compile once in its sequential integration, run `XgMoleArithmeticTests` plus existing `XgArithmeticTests` and `XgMiniPayTests`, then run runtime pause/hide/restart/manual-vs-auto and visible formula checks. The independent harness has already run all 27 tests across those three classes successfully; desktop rendering and Unity integration are still unverified.

`refresh_artifacts.py` only refreshes patch/manifest/metas. `audit_candidate.py` performs static/reference checks. `prepare_candidate.py` was a one-time scaffold and **must not be rerun** because subsequent candidate fixes are already applied.
