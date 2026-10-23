# 3df auto-merge UI preservation audit

2026-10-20. Read-only inspection of the current `competition-integration-20261020` sources after the merge. No source edits, Git operations, Unity process, test execution or package creation. The root's BoardData conflict and other agents' question-bank / Forget Night work are outside this review.

## Result

No lost integration hook or newly demonstrated functional regression was found in the requested UI features. The requested preservation checks below pass **static source inspection only**; this is not a rendered-UI or runtime PASS.

| Required behavior | Current evidence |
|---|---|
| Data installation hint | `XgTrainPage.cs:160` still calls `BuildDataInstallHint`. `XgTrainPage.Parts.cs:11` still calls `RefreshDataInstallHint`; this is reached through `Refresh` → `RefreshSide` → `RefreshTrainingParts`. The dedicated 84px banner still shows pending/installed counts, the “资料文件夹 → 训练数据” route and Ctrl multi-select instructions. It is gated by actual `CanInstallData`. |
| Actual installation path | `XgTrainPage.cs:63–70` keeps the root `XgDataFileDrop`, current bound Sim, toast notification and `view.Refresh(true)` callback; the hint is inside this real drop target and creates no alternative installer. |
| Original 24 rare-item entry | `XgTrainPage.Side.cs:70` still builds the independent 60px `BuildRelicAccess` card. `Parts.cs:12` refreshes it. `Relics.cs:18–48` keeps the click-time profile/backpack gate, routes to `backpack`, and displays `relicOwned.Count / XgRelics.All.Length` plus actual equipped/capacity values. The tooltip still describes the original 24 items. |
| 12-slot expansion | `XgSim.Relics.cs:60–63` preserves the two expansion IDs and 4/8/12 capacity. `XgRelics.cs:75` retains `Slots = 4, MaxSlots = 12`. `XingGuangView.Relics.cs:45–49` still pages 1–4 / 5–8 / 9–12, and actual locked-slot checks use `RelicSlotCapacity`. The train sidebar's separate `GearTiles = 12` is not confused with rare-item ownership or capacity. |
| Independent reading | `XgTrainPage.Books.cs:14–30` still routes the Books button and fallback “Open reading window” button to `XgReadingWindow.Open`. The old embedded panel only provides a summary; it is not restored as the reader. `XgYyTalk.cs:147,164,180` keeps book notices, owner rebinding and duplicate-completion routing, so YY reading answers remain connected. |
| Per-message AI indication | `YY2016/YYChatView.cs:417–425` still appends `AiReplyNotice:<sender>` after every non-player message, calls `AiContentNotice.BindFooter`, and reserves vertical space. The page-wide footer remains separately at :206–213. `AiContentFooter` keeps the language-change refresh. No merge change reduced this to only a one-time/global notice. |
| Prologue speech interruption guard | `PrologueDirector.cs:189–194` now retains `said >= 0 && said < length`, followed by complete text, the existing hold and fade. Approved `VoiceLines.Play` / BlipTalk behavior remains; no recording restoration or boot/shutdown change occurs here. |

The existing `RefreshTrainingParts` call is essential: the hint and item-access refresh calls are intentionally in the partial Parts file, not directly in `XgTrainPage.Refresh`. They have not been dropped by the merge.

## Incremental refresh / performance review

- `XgLabelPage.Tick:613–652` now calls `RefreshDeskGame(false)` while retaining `deskGame.Tick(dt)`. Full `XgLabelPage.Refresh` calls `RefreshDeskGame(true)` and still refreshes the task hall. The game helper compares owner, selected game and visibility and will open/rebind the game when necessary; it only skips repeated text refresh on the lightweight tick.
- `XgDeskGamePage.Tick:248–254` independently checks owner and language, refreshes money/day text every 0.25 seconds, and ticks focused gameplay every frame with its existing clamped delta. Thus the lightweight parent path does not stop gameplay or permanently freeze visible text.
- Label tick still processes card identity, owner/mode changes, quality/captcha transitions, input, timer bar, translation/memory animation and answer-focus state. Red-flash paper-theme repaint is restricted to the active flash interval, including its final settling tick. `RefreshAi` redraws the decorative spider only when its home/away state changes; ordinary text continues on full refresh.
- `XgTrainPage.Tick:305–323` retains rhythm, exam, data, pace and glitch animation paths. Expensive summary/scale/gear/book/ledger text is reached through full refresh instead of redundant per-frame rebuilding. `XingGuangView.cs:393–394,485–497` still refreshes the active page every 0.2 seconds, and purchases, training start/completion and installation callbacks force immediate refresh. The hint/item counts therefore have both scheduled and transaction refresh routes.
- `XgTrainPage.Sheet.cs:30–45,158` caches evaluated presentation values (question, answer, label, kind, dirty/right flags, language, highlight and texture identity). Its source comment and call order keep simulation/dirty-data/texture evaluation before the early return, so unchanged TMP/texture assignment does not conceal a newly installed image, language switch or changed answer.
- Actual TestSet construction/cache correctness belongs to the root's active BoardData reconciliation. The UI review does not certify that unfinished conflict or compare an intermediate cache implementation as a final result. Current `XgSim.Board.TestSet` and preparation paths use the same dataset/level/month/salt key and retain `TakePreparedTestSet`; no UI-specific substitution of training samples was introduced in the inspected paths.

No conflict markers were found in the requested XgLabelPage / XgTrainPage source family. No performance gain is claimed without a measured run.

## Non-blocking existing-policy wording observation

`XgTrainPage.Refresh:431` still describes a locked order button as “第4章开放订单 / Orders in chapter 4” (or a grade gate), whereas the already-approved core policy opens the order requirements page in stage 3. The actual button enablement uses `FeatureVisible("contracts")` and therefore does not bypass signing quality or delay the actual stage-3 button. This is a source-visible wording inconsistency; this read-only audit did not use Git and does not attribute it to the new merge. Record for the Claude/DeepSeek handoff rather than expanding this round.

## Handoff boundary

Remaining authoritative work belongs to the coordinator: finish the in-progress merge validation and existing UI/test run, then pause and hand off to Claude/DeepSeek as requested. This audit starts no new feature, background run, test suite or release. If runtime validation is not completed before the pause, report that limitation rather than calling this static inspection a Unity/UI acceptance result.
