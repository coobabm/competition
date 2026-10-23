# C5 shared arithmetic training input — source-only followup

Prepared 2026-10-19. No production edits, Unity, .NET compilation, model training or test execution. This is a small followup to the unapplied `arithmetic-stage/production.patch`, not a replacement for it. Presentation, calculus sign layout and hit effects remain outside this package.

## Decision A and exact boundary

Parent confirmed A: use one label-blind visible arithmetic encoding for actual teaching, training and automatic inference. Labels remain supervision targets. Do not compensate for changed scores by editing pass thresholds, sampling rules, assessment questions or model weights.

`XgArithmeticInput.Input` reads only canonical authored `question` and `category`. It tokenizes without calculating any answer. Literal operators are escaped to avoid concept-key syntax; visually equivalent operator glyphs normalize. Whitespace does not shift positions: positions are token ordinals rather than character offsets. The cold-guess seed hashes that same canonical token stream. The shown RHS is part of the visible claim, while the true result/explanation is absent.

`XgBoardData.FromCard` uses that input for ordinary arithmetic, then attaches `card.truth` as supervision and the source card only for existing diagnostic rendering. The special shutdown card retains its existing route. Other datasets retain their previous encoding. `XgMoleInference.Input` becomes a compatibility wrapper around the same input function.

This reaches the existing paths without changing their settlement or supervision rules:

- `TeachingGuess` and `PredictMole` read identical arithmetic features.
- Accepted real desk teaching reaches `TeachBoard -> FromCard -> Board.Train`; its label is attached after input encoding.
- Background arithmetic training reaches `PoolCardAt -> Make -> FromCard -> Board.Train` with the original `XgArith` questions and original target/noise policy.
- Fresh correction/checkup/sheet encodings through `FromCard` use the same visible features.

No solver, correctness RNG, injected trained weights, background microcalculus truth feed or scoring adjustment was added. Automatic inference has no training side effect.

## Model and assessment limits

The old arithmetic branch of `Logic` created “looks correct”/“checking succeeds” clues from `truth`; that path does not support honest label-blind inference. Removing it can lower scores substantially. Previously saved high scores and checkpoints are not new evidence for this input representation. Existing category-only concepts may still act as learned priors; old label-clue combinations no longer match arithmetic inputs. We do not delete shared logic weights or rewrite old scores in this narrow patch.

No `XgArith` generator, `TestSet`/exam seed domain, sampling implementation, truth label, pass threshold or reward code is edited. Since existing duplicate exclusion uses encoded signatures, the concrete rows selected by an existing dynamically rebuilt assessment can change when its signature becomes truthful visible input; do not compare its old and new aggregate scores as an efficacy measurement. The independent experiment below freezes new held-out rows before learning and never uses the existing displayed assessment for tuning.

The current `XgArith` pool includes basic arithmetic, powers and several other topics, but not the new calculus types. This patch repairs train/inference vocabulary alignment; it does **not** prove learning efficacy or generalization to calculus. The current concept board may fail even basic held-out arithmetic. Results must be preserved if it does.

Old `recentCorrections` rows contain already saved feature lists. They remain historical records and are not rewritten or relabelled by this patch. A newly encoded version of the same visible arithmetic claim uses the new route.

## Files and application order

1. Apply the original `arithmetic-stage/production.patch`.
2. Apply `training.patch` (4 paths: shared encoder+meta, FromCard hunk, inference wrapper).
3. Optionally apply `validation.patch` (4 new test paths including metas).

`base/` contains the exact two source baselines. `XgSim.MoleArithmetic.cs` comes from the original C5 candidate; `XgBoardData.cs` comes from current production. Prefer localized hunks. `manifest.json` records hashes. Patch applicability is checked only against those isolated layered baselines, since production does not yet contain the original C5 file.

## Prepared verification, not executed

`XgArithmeticInputTests` prepares five fast regressions: counterfactual label/seed/explanation mutation; spacing/operator equivalence without changing operand order; genuine existing pool parity; accepted `TryAnswerStatement` updates symbol concepts actually read by automatic inference; opposing supervision targets change real trained scores without changing inputs. No test inserts concept weights to make the model look capable.

`XgArithmeticLearningExperiment.FrozenUnseenClaimsBeforeAndAfterRealBoardTraining` is explicitly opt-in. It must not run automatically during this source-only task. It fixes salts 17/31/47, 640 training rows from the original generator, one declared model setting, checkpoints at 0/128/320/640 and 200 unseen original-generator claims per salt. A second set contains 128 unseen new-mole claims per salt. All splits are frozen before training; full input duplicates and equation left-side duplicates are excluded. It fails if the holdout cannot be filled, instead of backfilling training examples. Labels are not used to resample a favorable balance.

The experiment invokes actual `Make -> FromCard -> Board.Train`, then predicts using the automatic input wrapper. It logs split SHA256 values, all training rows, before/after per-case predictions, confusion counts, accuracy, balanced accuracy and guess counts. Each mole kind is also reported separately. A frozen diagnostic criterion requires mean balanced-accuracy gain of at least five percentage points on both original-generator and basic-mole unseen sets; failure remains failure. This criterion is not a game pass threshold. No parameter/seed search or repeated tuning against these same holdouts is authorized.

This is a controlled board-learning experiment, not natural gameplay progression or UI evidence. Word problems without `=` are deduplicated by complete visible wording; paraphrase-equivalent problems are not identified, a stated split limitation. Single-label per-kind subsets produce undefined balanced accuracy; retain that result rather than adding synthetic counterexamples. The calculus slice is out of training coverage and must never be reported as trained calculus proficiency.

## Later explicit teaching/correction of calculus: interface study only

Existing `RecordTeachingQuestion` records a notebook row; it does not train. Existing `TryAnswerStatement` only owns the current desk card and cannot safely accept an arbitrary notebook card. The notebook also does not persist arithmetic category. The current mole report callback does not return an accepted settlement receipt directly.

A future minimal extension needs a real UI event that supplies an explicit true/false player label plus a current question token or persisted notebook ID. Preserve canonical question/category, validate that owner and label event, receive an accepted result, and call existing `TeachBoard` exactly once with that player-supplied target. Keep rewards in the existing settlement path. The source problem may be calculus; the input adapter already supports its visible symbols without knowing the answer.

Do not infer “false” from a mole being left alone or expiring. In `arith_mole`, a manual whack only asserts “true”; automatic whacks are model outputs, not human supervision. Do not train on generated correct answers just because they appear in a notebook or an auto-action was marked correct. Until there is an explicit correction/label UI and a receipt/ownership contract, no unused public mutation API or silent calculus supervision is introduced here.
