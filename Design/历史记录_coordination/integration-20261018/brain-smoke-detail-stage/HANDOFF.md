# Brain detail acceptance migration (2026-10-18)

Apply `brain-smoke-details.patch` AFTER the prior `brain-smoke-current-gates.patch` and `brain-smoke-runnerfix-stage/runnerfix.patch`. All are narrow diffs; do not overwrite entire files. Main runner patch uses saved post-gates/post-runnerfix base. Other files diff against working main.

## Implemented scenarios

- abilities: purchased brain fixture. Retain real lamp colors/labels, learned ability count, no premature learning, production ConfirmNextStage, no phantom seventh, save-switch hiding. Replace obsolete fixed 50%/62.5% threshold bars with current real ParamsFill/SamplesFill (log scale); populate eligible fixture from actual current thresholds and data weight, not hard-coded6000. Explicit boundary fixture still not natural playthrough.
- region-cards: actual brain region click and current architecture lore/changed-architecture refresh/close/tone axes/unchanged learned board/old-save cleanup remain. Ownership assertion now requires details inside purchased window but outside scaled drawing. New actual client bounds assertion.
- answer-path: purchased brain, retains actual settled wrong input/real used concept IDs/edges/expiry/unscaled time/quiet/reduced/bounded4paths/unsubscribe/no old-save callback. Wrong objective UI input now answers statement truth, never evaluates AI correctness. Obsolete DesignFirstLessonChecks wrapper replaced by shared real12-question fixture.
- answer-correction: same purchased fixture and actual current False input, original0.4+0.6s natural lifetime, correction pulls, paused/reduced and wrong-answer-no-reward assertions retained.
- growth: removes old unconditional SKIPPED success in active #else. Actual existing merged tree/items/abilities/growth aliases, no duplicate old pages/nav, removed consumables alias grants/spends nothing, six actual cards in correct order, ZH/EN names/current count/knowledge-gated thresholds/TMP overflow, state purity, production insufficient-funds rejection/charged single purchase/no second charge. Original discontinued consumable branch retained under existing compile symbol for traceability; not re-enabled.
- lesson: original mode is per-teaching pulse (NOT knowledge-card popup). Migrated purchase fixture, direct statement input, same-frame learned cells, correct reading region/pixel pulse, unscaled natural fade, wrong input no success pulse, disable/re-enable lifecycle, save detach and new-owned-save rebind, destruction callback cleanup remain. Old completed save does not automatically get new purchase; explicit owned restoration fixture supplied.

## Production fixes discovered by retaining real input assertions

1. XgBrainWindow: attach existing lightweight XgBrainWallpaperHover to opaque client Image. Previously WindowMode deliberately disabled all drawing raycasts and no client handler replaced wallpaper context relay, so hover/region click were unreachable. Drawing remains non-editable; title/close/resize controls remain separate.
2. XgWallpaperRegionCard: derive width/height and clamp position from actual client, adapt body text fitting. Fixed390px card previously exceeded320px initial brain window. Large fixture still gets normal390px card; default client gets bounded card. No full desktop wallpaper restored.

## Validation

Before parent's upstream merge, Core/UI/Editor safe isolated compile exit0 with all above except the subsequently added lesson partial. Logs then overwritten by honest latest attempted compile: during parent merge source contained conflict markers and temporary missing ImageKind/PeriodMeme APIs. These are unrelated active integration, not subagent changes. Final compile needs rerun once parent confirms stable; no Unity run by subagent, no runtime PASS claim.

`compile.py` redirects all compiler outputs to staging and compiles current Core, UI, Editor sequentially. The patch preserves existing assertion intent; any latest runtime discrepancy must be inspected rather than blanket skipping.

## Knowledge vs lesson coverage boundary

KnowledgeVisualsQa covers portrait card/emphasized terms/collect flight/atlas+badge/one-time text/English/saved honest curve halo and no audio request. BrainWindowPerformanceQa covers knowledge notification feedback route. Neither proves the old per-question LessonPulse hook, real pixels, event-detach or time-scale behavior; the migrated lesson mode exists specifically to retain these separate tests. Their previous result snapshots are evidence only for their own tested revision, not current overall completion.

## Final static compile after upstream merge

2026-10-18: Core/UI/Editor all exit0 against stabilized latest main, including new upstream Core sources missing from earlier Bee response list, all six migrated scenarios, pulse lesson and FamilyStar fix. `brain-window-family.patch` adds the previously omitted existing FamilyStar to InstallWindow (one line), retaining family belief visualization without restoring old wallpaper overlays or duplicate rebirth systems.

Mouse routing review: relay attaches to existing opaque Client Image; it adds no rectangle over title/close/pin/resize siblings. Detail close Button consumes its own click before ancestor handler; scaled drawing stays non-raycast/non-editable. Actual raycast/input and obstruction claims still require parent Unity runs; no physical/manual input or screenshot review done here.
