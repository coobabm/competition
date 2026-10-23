# Economy / Art / Desktop final independent review

Reviewed 2026-10-03 against the integrated source in `RipplePrototype/Assets/LingGuangV05`. Source is frozen after the explicitly authorized last fixes. This review does not claim a full natural playthrough or real-model success; parent owns the unified Unity run.

## Delivered and checked

- Economy has four working uGUI pages, persisted economic state, shared Formula inputs, card corrections, optional confidence/expression providers, dataset collection, jobs, market prices/resale/repair, electricity, heat/throttling, hosting and offline settlement.
- The starter GPU and truthful manual-label income remain available to recover from zero money or a power outage. Wrong labels and repeated claims do not create money.
- Cases/capacity/cooling/hosting/overclock now require the real hardware unlocks. Overclock has speed, power and heat consequences.
- Pipeline now requires chapter 2 plus its unlock plus its switch. Batch unlock raises each heartbeat's processing maximum from one to four cards. Unpurchased notebook no longer records mistakes; first-correction moment and bonus still work and UI directs the player to the skill tree.
- Review multiplier applies once to raw samples and their learning points, including offline dataset collection after Main injection. Confidence `null` stays visibly estimated and retries without rerolling the displayed answer.
- Offline report supplies chronological ≤10-second slices and day boundaries, including outages. MainRoot forwards steps, elapsed days and report identity to Exam; Exam clips contract work at the deadline and persists a receipt before settlement.
- Art: 617 authored SVGs retained and rendered to 617 PNGs; 175 nine-slice definitions; audit reports no XML/PNG errors, no saturated-red usage and no missing P0 runtime sprite IDs. Desktop uses actual wallpaper, icons, windows, buttons, taskbar, title controls and Lao Zhou portrait.
- All five Desktop runtime `GetComponentInChildren<TMP_Text>` lookups now include inactive children. This prevents hidden/minimized windows from producing null text lookups during progression updates and viewer opening.

## Verification evidence

- Latest Economy NUnit-method harness: **18/18 passed**. Includes new pipeline/purchase/switch/batch/outage guards, notebook purchase/first-moment behavior, hardware and offline slices.
- Full Economy root compile against installed Unity 6000.4.5f1/UI/TMP assemblies: **0 warnings, 0 errors**.
- Full Art runtime + Editor + Desktop runtime + Desktop Sim + Core compile against installed Unity assemblies: **0 warnings, 0 errors**.
- Parent reported 119 real Unity tests passing before the last two Economy tests and inactive-viewer fix. Parent must rerun final validation against the frozen source; the 119 number is not represented as verification of these final edits.
- Art audit file: `Assets/LingGuangV05/Art/Tools~/audit-report.json`. Unity import validator: `LingGuangV05.Art.Editor.ArtValidation.ReimportAndValidate()` (617 Sprite imports and 175 borders). The compile and offline export audit do not replace Unity import validation.

## Quantitative endgame check

The loaded `Content/order.json` specifies five batches of 10k/50k/200k/500k/1m, quality lines .80/.82/.85/.88/.90, each seven game days. `Batch.Size` applies to **each** active queue, so the final contract represents six million queue operations across six services on a million messages. UI should communicate that interpretation clearly.

Seven days are 2,100 real seconds. At five items per training step and perfect quality, the final contract needs at least 571.43 heartbeats/s. One feasible idealized late-game configuration:

| Quantity | Result |
|---|---:|
| 28 C2050 cards, 14 cases, cooling, hosting | 8,474 W under 20,000 W |
| Three research speed bonuses, no overclock | 602.784 heartbeats/s |
| Chapter 6 hosted target temperature | 57.90°C, below 75°C throttle |
| Equal allocation, weakest queue quality .95 | 1,002,128 items per queue in seven days |
| Hardware capital at base prices | ¥534,700 (market prices vary) |

Thus the million-message objective is numerically reachable within the power limit. This is an analytical capacity check, not a claim that the pacing or full progression route has been playtested. Reply quality depends on expression/personality/fans; expression 8 and at least 200 fans gives .90 for an ordinary personality, while expression 8 and 300 fans gives .95. Training quality in the other queues still needs sufficient skills, parameters, steps, samples and structure bonuses.

## Remaining prototype boundaries

1. Offline time advances economic steps/jobs/datasets/self-labels/contracts, but does not simulate the live pending card pipeline. A player using only automatic cards receives fewer offline samples than the equivalent online session. This limitation is now stated in Economy integration documentation.
2. `training.review` currently increases Exam correction count through `Rules.Fixes`; its skill-tree text calls it review-card capacity. The review-card collection itself is uncapped. This is a remaining description/behavior mismatch, not a missing button or blocker.
3. MainChat's imitation sample reward originally raised `skillSamples[Mimic]` without global `samples`, making Economy and Exam sample-share denominators diverge. Parent was notified and owns the correction; check its final source/result rather than treating this independent report as verification of that edit.
4. Expression 6–8 purchases require successful chat counts. If the local talk model stays unavailable permanently, advanced expression progression cannot be reached naturally and final reply quality may be blocked. Model failure is displayed honestly; no simulated real-model success is claimed here.
5. Art design mockups 9.2.4/5/6/7/12 and five actual main-scene screenshots remain separate from the complete runtime sprite delivery. Section 10 animation descriptions require runtime interaction verification; exported sheets alone do not prove every animation is implemented.

No VM setup or repair was attempted. Sources from failed Claude output were preserved before adaptation; the final integration module should not be mixed with its old overlapping Economy classes.
