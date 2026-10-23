# Legacy LocalBuild / brain smoke migration audit

This is an inventory, not a claim that all LocalBuild checks passed. Snapshot taken 2026-10-18 while main was actively integrating changes. `localbuild-entrypoints.json` lists 110 public zero-argument entry candidates with source lines; utilities/helpers are labeled. `observed-result-files.json` preserves 50 historical result/report files. File existence, old PASS and successful compilation are not present-revision runtime acceptance.

## Patch delivered

`brain-smoke-remaining.patch`: ONLY DesignWallpaperSmoke.cs, staged outside main. Adds outputs/persona/family/rules to existing real-purchase fixture. Keeps original functional/pixel assertions. Save replacement assertions now verify window/drawing/taskbar hidden and ownership absent: hidden windows intentionally stop mesh updates, so requiring hidden caches to zero would contradict current performance design. Full Editor assembly independently compiled with actual ScriptAssemblies references, exit 0; warnings only. No Unity execution or screenshot validation by this agent. Apply via `git apply --ignore-space-change`; main CRLF means normal whitespace check may be noisy.

Run separately, retaining result/checks per mode because legacy runner overwrites a single output folder:
- DesignWallpaperSmoke.RunOutputs
- DesignWallpaperSmoke.RunPersona
- DesignWallpaperSmoke.RunFamily
- DesignWallpaperSmoke.RunRules

Potential genuine failures must be investigated, never blanket bypassed: latest family SMS subjectivity, bounded board geometry, skin/pixel scaling, persona axis behavior. Those original assertions remain.

## Mode disposition (all identified Start modes)

| Mode | Status / current requirement |
|---|---|
| concepts / links / regions | Main already migrated to purchased fixture by parent. Reuse actual parent reports; this audit does not add a new PASS. |
| outputs / persona / family / rules | Migrated in supplied patch; Unity pending. Preserve stage port forms, learned axes/feedback, family preference star and protected rivets. |
| seed | Still pending: migrate initial purchase AND reloaded completed save must actually own brain.window. Preserve protected seed/reset/brightness assertions; do not pretend every old completed save owns new purchase. |
| abilities | Still pending: legacy ConfirmNextStage/bar threshold fixture may not satisfy new gameplay gates. Preserve only confirmed six-ability lights and independent current parameter/data progress; obtain eligibility through production progression. |
| hover | Still pending: purchased window defaults always on top. Old app coverage scenario must explicitly unpin brain window and order app above it before asserting no hover leaks. Keep actual relay, counts, rich-text escaping and deleted-concept checks. |
| region-cards | Still pending: replace obsolete parent-location/pin-copy assertion with actual independent window card ownership/clipping/lifecycle. Keep region lore, updating architecture, close and no state edits. |
| region-cards-visual | Still pending: related pixel capture expects opaque wallpaper filling desktop; change probe to actual purchased client + card, retain 180-frame stability measurement. |
| readonly | Still pending: moving/resizing window is permitted, editing learned concepts by drag is not. Existing no drag/drop assertion must scope drawing descendants, not window shell. Retain real input and unchanged board checks. |
| lesson | Still pending: knowledge first-show callbacks remain, but atlas/card/flight redesign supersedes old card details. Map valid pulse and no duplicate learning to KnowledgeVisualsQa plus brain-window feedback checks; do not declare entire legacy mode passed. |
| answer-path / answer-correction / growth | Still pending: genuine learning trace, correction/replay and long-lived learned geometry remain useful; migrate fixture/obsolete wallpaper pixel coordinates, review latest question truth rules. |
| tech | Still pending legacy wrapper (calls old wallpaper step11). Current TechTreeRuntimeChecks / current parent tech UI report cover newer rules; older detailed cases still need one-by-one mapping before retirement. |
| sense-start | Still pending and partly superseded: old 4-sample training unlock is invalid. Keep common-sense default/matching-dataset checks; use 12 answers → tech → paid curve for training. |
| lesson-goal | Old four-card lesson goal is superseded; current twelve-question objective is required. Old '/4' assertions must NOT be relabeled as current acceptance. |
| first-minute (five scenarios) | Mixed: old first-minute reveal/four-answer train timing superseded; idle must not create progress, real correction and pending chat remain. Needs scenario mapping, not deletion. |
| all / pin / desktop-fade | Old automatic wallpaper reveal and 30%-alpha overlay removed by explicit user requirement. Current replacement is ordinary wallpaper, purchase-gated opaque brain window, window pin/minimize/taskbar/reset. BrainWindowPerformanceQa has 41 actual assertions at 070251; this covers window lifecycle but is NOT an execution PASS for these old modes. |
| menu | Old desktop context-menu View/Quiet on brain wallpaper superseded. Ordinary desktop native menu must remain; brain window controls are separate. Future test should distinguish native menu preservation from removed wallpaper actions. |
| motion / wiring-motion | Independent empty-scene motion fixtures; no automatic reason to retire. Need actual runtime evidence at current revision. |
| wiring-scene / training-scene | Still pending fixture migration; learning/wiring effects can remain relevant in purchased brain, but post-setup wallpaper assumption is invalid. |

DesignWallpaperChecks.Run itself tests XgWallpaperReveal, a legacy helper. Passing it proves that helper only. It does not prove automatic reveal belongs in current game. Current purchased-mode bypass is deliberate replacement by purchase/ordinary-wallpaper assertions, not silently counting the old assertions as passed.

## Existing actual evidence to retain (known scope)

- BrainWindowPerformance/20261018-070251-936/result.json: 41 passed, purchase/window/control/performance gates/eight feedback routes; no physical player input automation.
- WorldCompleteness/20261018-072836-460/result.json: 226 passed, no PNG.
- IdleSaveAndStageSnapshot/20261018-071306-185/result.json: 22 passed including disk stage snapshot roundtrip; timer boundaries accelerated.
- StageCurtain/20261018-065246-992/result.json: 95 passed, older than snapshot change; no PNG.
- PhotoLibrary/20261018-070833-905/report.json and TeachingTools/20261018-071458-655/report.json: actual respective UI checks; no claim about 13 generated group photos.

Newer parent reports may supersede these. Do not use this file to report every Assets/LocalBuild/Editor check complete. Remaining legacy modes above are explicit pending work, with obsolete requirements tracked instead of erased.

## Follow-up implementation: current gates and remaining brain scenarios

Use **brain-smoke-current-gates.patch**, regenerated against current main after parent applied first four-mode patch. This supersedes the original patch for subsequent work. Five changed source files plus XgLabelPage production fix. Independent current Core, UI and Editor compile all exit 0 (logs named *.compile.log). No Unity runtime execution by subagent.

Implemented:
- Purchased fixture grows to actual 960×640 with negative resize Y; asserts actual rect after Canvas update.
- seed: actual purchase fixture, protected question/data/pixel assertions retained; switching to unowned save verifies hidden old drawing/taskbar; restored fixture explicitly owns curve+brain.window rather than granting it to every old save. Rendered seed must bind replacement board.
- hover: actual purchased drawing; turn off default always-on-top before app coverage case, move common app ancestor above brain and assert sibling order; retain real pointer relay, changing model count, legacy unknown count, rich-text safety, removed-concept tooltip tests.
- readonly: only drawing forbids concept editing; actual window motion additionally must work and preserve serialized board; original click/drag/region-card/quiet assertions retained.
- sense-start: untouched current opening and actual 12 pointer answers, exact accurate sample increment, no unpaid train; actual charged curve purchase and correct dataset handoff. Removed learningText/companion no longer exist in live page: replacement checks all actual saved sheet question/answer rows against authored common-sense bank. Stage2 arithmetic and old unlocked desk preservation checks retained for honest runtime validation.
- lesson-goal: wrong answer counts one completed question, grants zero sample and advances immediately; 11 right answers reach 12; nav gate rejects unpaid training then accepts bought curve, money actually deducted; visible progress/readability/geometry/locale and state preservation retained; resumable new-flow 2/7/12 snapshots cannot acquire unpaid curve; trained save returns metadata.
- production XgLabelPage: first goal shows completed questions/12 for new flow; old non-flow saves retain prior sample-goal meaning. Teaching instructions stop directing player to removed right-hand companion: bilingual remaining-question / buy-curve / training-open prompts.

Scope honesty: old companion button click and legacy learningText snapshot are explicitly replaced by current nav and answer sheet assertions, not counted as passed. Production gates and zero-time untouched opening are used; stage2/legacy snapshot sections are explicit boundary fixtures, not a full playthrough. Goal tests check actual UI text and LCD click but execute curve purchase through production BuyNode, not simulated pointer tree-node purchase (separate tech UI acceptance covers that).
