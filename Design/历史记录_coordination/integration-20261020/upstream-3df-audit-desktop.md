# Upstream 3df48181 — desktop / prologue / story-text audit

2026-10-20. Read-only inspection of `competition-integration-20261020`. Compared `c5c63198..refs/remotes/ripple-review/lingguang-v05` (`d371a2b7`, `3df48181`) and the current working tree against `3df48181`. No source edits, Unity process, test run, commit, packaging or release. The coordinator's existing tech UI run was not touched.

## Integration disposition

| File | Upstream change | Current integration state | Recommended action |
|---|---|---|---|
| `Assets/LingGuangV05/DesktopBridge/Story/PrologueDirector.cs` | d371a2b7 adds `said >= 0` to the speech-following typewriter loop | Current line 192 still lacks this guard | Integrate the small upstream hunk. This is a coroutine termination fix, not a voice/style/plot change. |
| `Assets/LingGuangV05/DesktopBridge/DirectDesktopFullscreen.cs` | 3df48181 disables every scene camera except `lcd.sourceCamera`, and also deactivates `LCD Studio Lighting` | Current helper lacks these additions; neither current source nor the 3df source has a caller of `Install` | Acceptable as a dormant helper update if desired. Do not add a startup call, re-enable this route, or change the physical monitor path. No active improvement should be claimed from merging dormant code. |
| `Assets/LingGuangV05/Resources/LingGuangV05/Story/six_stage_lines.json` | Deletes two unused inner-thought keys | Current resource already has the exact same text change; no diff against 3df48181 | Nothing to import. Preserve current file. |
| `Assets/LingGuangV05/Resources/LingGuangV05/Story/era_advantage_lines.json` | Deletes nine retired group dialogue keys | Current resource already has all nine deletions, plus two local character-continuity corrections absent upstream | Preserve current file; do not overwrite from upstream. There is no new upstream text requiring a merge. |

## 1. Prologue coroutine: safe direct integration, limited claim

`Thinking` starts BlipTalk/voice playback, then follows `VoiceLines.Revealed`. At current `PrologueDirector.cs:189–193`, the loop tests only `said < length`. `VoiceLines.cs:29–34` explicitly returns `-1` if there is no timeline; `-1 < length` otherwise keeps the coroutine yielding and can set `maxVisibleCharacters` to a negative value. Upstream adds a `>= 0` guard, then the already-present code shows the complete thought, holds it and fades out. `ThinkAndWait` can finish instead of waiting indefinitely on this path.

The change leaves `VoiceLines.Play`, `UseBlips = true`, BlipTalk generation, audio assets and shutdown untouched. It is consistent with the approved protagonist muttering voice recorded in `Design/整合记录.md:759`.

Limit: this is not an ownership-token solution for every possible overlapping voice. A replacement BlipTalk line that still has a valid timeline can still be followed; `VoiceLines.Stop` currently stops audio but does not clear its timeline. The upstream hunk specifically fixes the no-timeline/negative-result path. Do not claim all overlapping-dialogue timing has been proven fixed without runtime verification.

## 2. Fullscreen helper: retain original monitor, no new activation

The helper already reparents the physical LCD display to a screen-space overlay, disables its old canvas and monitor-framing components, and hides the monitor model (`DirectDesktopFullscreen.cs:26–44`). The new hunk additionally turns off non-LCD cameras and the studio-light root. That is reasonable cleanup **inside an intentionally selected flat fullscreen route**, but activating the route would remove the room/monitor presentation the user explicitly chose to retain.

Current static safety evidence:

- `Install` has no initialization attribute and the static class is not a MonoBehaviour.
- Searching current `Assets/**/*.cs` for `DirectDesktopFullscreen` returns its definition only.
- `git grep -n DirectDesktopFullscreen 3df48181 -- '*.cs'` also returns its definition only.
- `MainSceneEntry.cs:27–35` initializes the existing direct Main path and hides a retired photo gallery; it does not install fullscreen desktop.
- `CaseMenuMainDisplay.cs:18–19,212–217` explicitly keeps the desktop inside the original physical monitor, with the room visible around it.
- `CaseMenuController.cs:308–327` preserves the accepted shutdown: physical LCD fade, then reverse camera flight. These code paths are outside the new hunk.

Therefore simply bringing in the dormant helper hunk does not select a different opening/closing setting. If later work proposes to **call** it from Main or the case-menu handoff, that is a genuine setting conflict and requires user clarification before activation. Recommended default under the existing explicit preference: preserve the physical-monitor opening and present shutdown without asking the user to reconfirm them.

## 3. Deleted story keys: already reconciled, no privacy/date rollback

The upstream nine `era_*` deletions are:

`era_skeptic_aj`, `era_skeptic_xg`, `era_rescue_aj`, `era_rescue_xg`, `era_team_aj`, `era_team_xg`, `era_changed_xg`, `era_changed_aj`, `era_changed_ask`.

The two `six_stage_*` deletions are `s2_name_think` and `s2_moved_think`.

The current working tree has already removed all eleven. `Design/剧情残留文本清理_20261019.md:3–17` documents them as unreferenced retired copy; the first nine belong to the old group-private-knowledge version. An exact-key search of current Assets `.cs` and `.json` found no remaining reference to these eleven keys. Current story steps still reference their valid retained receipt/thought keys. These upstream deletions therefore do not newly remove live dialogue or make a live key unresolved in the integration tree.

Ordinary group chatter remains in `Resources/LingGuangV05/YY/yy_group_presets.json`, including g03 overtime until nine and g06 working over the weekend. Its note still distinguishes public crowd work from private AI knowledge, and its date/event gates remain present (e.g. g16 requires the actual `volley_wake` event). Neither JSON diff edits group sender rules, story conditions, historical-news dates, preset minDate/month/after gates or Lili's runtime joining check. Do not broadly replace those other resources from upstream.

## 4. Two local character corrections must survive

Only two differences remain when comparing `era_advantage_lines.json` from 3df48181 to the current working tree:

| Key | Upstream | Current approved integration |
|---|---|---|
| `era_delivery_boss` | “我说是我表弟，学校里弄的” | “我说是我表弟下班后帮忙做的” |
| `era_identity_cousin` | “我还以为你在学校拉了个小组” | “我还以为你找了人一起帮忙” |

`Design/剧情残留文本清理_20261019.md:19` records the two matching English/Chinese corrections and explicitly preserves actual-delivery prerequisites and the cousin's private-AI knowledge boundary. Whole-file replacement would regress an employed protagonist to a school project and conflict with the already-approved setting. Preserve the two local lines and their current English counterparts; the upstream new commits do not actually modify these lines, so a normal selective merge has no reason to revert them.

## User clarification boundary

No new unanswered setting choice is necessary to integrate the safe prologue guard and retain already-correct story resources. The physical-monitor preference and employed-protagonist continuity are already resolved.

Ask the user only if the coordinator intends either of these new departures:

1. Activate flat fullscreen desktop, replacing the original physical-screen/room route.
2. Restore the school's project/team dialogue over the documented after-work version.

Both are avoidable by preserving current behavior. A helper import is not permission to activate it; an upstream text file is not permission to undo approved local character corrections.

## Remaining validation

After any code integration, coordinator should compile and verify the interrupted/no-timeline thought can finish, normal muttering/typewriter behavior remains, and the established boot/shutdown path is still selected. This audit does not claim those runtime checks have run, and it does not touch the ongoing UI session or the pending v6 title delivery.

Related earlier query: self-play is gated by **game time**, because `XingGuangController.cs:262` passes `GameCalendar.Now(runtime.Sim.S)` into `TickBoardSelfPlay`. The proposed logic4 Detail should explicitly say “游戏内22:00–06:00、保持游戏运行时” / “During in-game 22:00–06:00 while the game is running”, avoiding a real-world overnight-play implication.
