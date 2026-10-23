# B02 review

Reviewed staged implementation only; no main project edits.

- Fixed plateau classification: six finite samples within 0.002 range; a strictly declining series never counts as plateau. NaN/infinite tails cannot trigger regress/grokking/deep_worse. Stage 4 accepts null history safely.
- Missing reward host cannot consume a quiz or invent income. Wrong answers are consumed once, award nothing, and show explanation rather than implying retry.
- Old-save null lists repaired; collected cards restore discovered status; stale collected pending entries removed; unknown pending IDs retained for forward compatibility but skipped when displaying cards.
- Hide/reopen and simulation rebinding reset modal state and view key. OnDisable clears blocking state; hidden pages do not reopen modal during tree navigation. Existing FocusNode helper provides frontier fallback.
- StoryFocus and XgEmergenceCutscene.Busy both hold cards/quizzes and participate in refresh key.
- Resource loader rejects >4 choices matching the actual rendered controls.
- Added five focused tests (9 total), including invoking the actual stage-four epoch observer with null history. Core, desktop, and test assemblies compiled with Unity bundled Roslyn; no compiler errors. This is not Unity runtime verification and tests have not yet run.

## Integration
Use current four staged source/test files and B02-hooks-reviewed.patch. Do not run make-hooks.py: it regenerates older hooks and would discard hide-window handling. Core.rsp/B02.rsp were refreshed for newly merged source files.

## Remaining actual-trigger gaps
- shift and continues are explicitly not wired; do not fabricate these experiences. Their questions remain catalogue content until actual interactions report failures.
- trainingSmallSteps currently counts across tracks; consider separate counters if alternating tracks should not contribute.
- Real hidden window/reopen and overlay ordering still require Unity play verification.
