# C7 risk forecast and disclosed anniversary candidate

Staging only. No production writes, Unity, compilation, model calls or runtime tests were performed.

Apply `daily-risk-dates.patch` as a minimal patch; copy only the 5 new C# assets and metas in `new-files.json`. Do not overwrite newer existing files with full candidate versions. `manifest.json` binds the 6 existing-file baselines, `SHA256SUMS` binds all candidate files. `prepare_patch.py` refreshes exact hunks against current production and fails if anchors changed.

## Actual risk consumer

Root explicitly approved reusing the independently purchased `train.monitor` entry. No new risk technology, price, stage or prerequisite is introduced. Probability questions remain immediately available from `daily.dice` alone. A real `train.monitor` purchase plus `daily.dice` enables finer forecast text; legacy visibility by itself does not pretend that the monitor was purchased.

The existing training-page consumer now uses a read-only projection of the actual `TrainEpoch` / `StepAccuracy` rules. Previously it displayed `DropRisk.chance` without `RateSteady`, and displayed tear separately without showing that a tear skips the ordinary roll. The projection includes:

- Tear probability at the chosen rate, including clipping and warmup.
- Conditional ordinary-drop probability with the actual rate multiplier.
- The one-point meme-drift recovery performed before the ordinary roll in TrainEpoch.
- Exclusive paths: `tear + (1 - tear) * ordinaryRoll` for a score loss, subject to score floor and a charged residual guard.
- A charged guard or an already-minimum score prevents score loss but does not falsely erase the chance of a blow-up/failed roll.

All users see corrected whole-percentage values. Probability intuition adds 0.1-percentage-point precision and the conditional-path explanation; no deliberately wrong baseline or invented statistical confidence is used. The snapshot is explicitly for the current model/data/rate/guard setup, recalculated by the normal page refresh. Configuration changes before a round settles can change its probability. It is the game's random rule, not a prediction guarantee, empirical training benchmark, external model forecast or a change to training outcomes/RNG. Unknown/nonfinite input shows unavailable. Hardware heat warning remains its separate existing warning.

## Authoritative anniversary source and actual disclosure

New source discovery corrects the prior audit's missing-source conclusion:

`Assets/LingGuangV05/Resources/LingGuang/Rag/qingwen.json`, entry `us-anniversary` (around line 859) says: “你故意晾了他三分钟才回「好」，所以你坚持纪念日是1月1日，他非说是12月31日。” Adjacent `us-confess` gives the already-authored 2014 New Year's Eve confession. The candidate adds no relationship date or player history.

The reminder preserves the disagreement: “纪念日：晴雯认1月1日” / “Anniversary: Qingwen counts 1 January”. It appears within seven days only after an actual saved incoming text message from Qingwen affirmatively states the canonical 1 January date. Questions, hypotheses, quoted examples, other senders and the competing 31 December date cannot create that reminder. Recognized declarations are deliberately narrow; alternate wording can remain unrecognized rather than guessing a date. Birthday keeps its existing strict 27 September disclosure predicate.

Evidence is the existing persisted YY message with its original sender/text/game time; the canonical reference is `XgDailyKnownDates.AnniversarySource`. A cached read-only projection connects them, and the reminder tooltip shows the actual quote. No synthetic message, legacy disclosure flag, conversation completion, affection or gift is written. Existing real saved messages may serve as evidence; the existence of RAG data alone never counts as a conversation. Save/controller/chat ownership checks prevent projection across saves. Annual lookup handles the December-to-January boundary without manufacturing an anniversary start year or anniversary count.

## Checks and limits

Static `git apply --check` and UTF-8 LF generation passed. Nine new NUnit tests are prepared, **unrun**: dual-purchase behavior, rate correction, exclusive risk paths, floor/guard, projected meme recovery + unchanged save/RNG, invalid input, affirmative versus false date forms, year boundary, and the actual authored resource reference. An old risk-gap assertion is replaced by the newly connected gate assertions.

Root runtime work still required:

1. Open the real training monitor with Probability intuition unowned, then buy it and observe more precise values. At rate -2, score-drop probability must be zero; at positive rate, the tooltip must show exclusive paths. Charged guard and lowest score must show zero score loss while retaining nonzero tear probability where applicable. Verify CN/EN/narrow layouts, and that changing forecast precision does not change RNG, round outcome or price.
2. On an isolated real save with Care purchased, receive genuine dialogue through the normal YY path. Without disclosure, the date remains absent. With affirmative canonical incoming disclosure, the matching seven-day reminder and original-quote tooltip appear. Player-origin messages, hypotheses, quotes, wrong dates, absent RAG conversation and a different save must remain absent. Test Dec 25 / Jan 1 rollover and save/reload.
3. No actual spontaneous anniversary disclosure or real reminder screenshot is claimed by this staging work. The authored source and consumer are connected, but runtime evidence remains pending.
