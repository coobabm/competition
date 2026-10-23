# Life event host/settings candidate — 2026-10-19

Candidate only. No production edits, compiler, Unity run, or Windows desktop minimization performed.

## Confirmed scope

- User decision A: only minimizing the entire Windows game window pauses event active time. An unfocused game and a hidden/minimized in-game AI exe continue counting. No `Application.isFocused`, foreground-window lookup, or `WindowOpen` gate is used.
- Current-save settings: 8 minutes / 15 minutes / off. With no usable simulation, the control is disabled and says “进入游戏后设置 / Enter the game to configure”. New saves default to 8 minutes through the Core candidate; existing save modes are never overwritten by a global preference. No Runtime fresh-game inference or identity marker was added.
- Core interface agreed with tech_pan_candidate: `LifeEventActive`, `LifeEventConflict`, `LifeEventIntervalMode` (0/1/2), `ObserveLifeEventConflict()`. Core owns serialization, result acknowledgement, cooldown, migration and candidate-pool semantics.

## Files and integration

- `XingGuangController.cs`: adds one activity clock per Controller and calls it immediately before the original simulation dt. Calls the no-tick Core conflict observer before the stage branch skips Tick, and on SaveSlotsPanel early return; the latter also records an explicit pause for the next recovery frame. Sim.Tick dt, training/preview work, economy and preload code remain intact. A setter changes the current save mode through the Core API and invokes the existing SaveNow path; unreadable-save fallback simulations cannot be edited.
- `PlayerWindowState.cs`: reuses the current-process `MainWindowHandle` discovery pattern from existing `DesktopBridge/PlayerWindowSize.cs`. Checks IsWindow, owning PID and UnityWndClass before reading IsIconic. Cached native reads are cheap; failed discovery retries at most once per second. Editor-only probe supports tests without minimizing anyone's desktop. Unsupported/non-Windows or unavailable native detection returns false and makes no focus-based guesses.
- `XgLifeHostActivity.cs`: native minimization/explicit pause -> Active=false; StoryFocus/YY group scripts/registered reader windows -> Conflict=true. Only the first frame after an observed pause/minimization is skipped. Ordinary long frames stay active; runInBackground is enabled, and elapsed frame duration is not evidence of minimization. The original simulation/economy dt is unchanged.
- `YYChatHub.GroupPresets.cs`: exposes group playback status, including script handlers bound to a group. Root additionally authorized a narrow coroutine-lifetime repair: disable stops its coroutine while retaining undelivered replies, enable resumes the same queue with the original 2–4 second delay; an owner state and generation prevent stale disposal from clearing a resumed coroutine or carrying queued replies into another save. Normal active delivery content, order and waits remain unchanged.
- CaseMenu HUD/Controller: an additional bilingual settings row with explicit keyboard navigation; reads the currently loaded save again on open and after change; persists via current simulation save. A failed write reports the failure rather than claiming persistence.

Run `refresh_patch.py` to validate untouched production baselines and regenerate patch/manifest. `prepare.py` is the one-time candidate construction script and should not be rerun on already edited candidates. Because source files contain CRLF/mixed endings, the successful read-only applicability check is `git apply --check --ignore-space-change --whitespace=nowarn production.patch`. Baselines and manifest are recorded for reviewed integration, not authority to overwrite unrelated edits.

## Explicitly not wired / risks

1. No independent reader window exists in the reviewed production code. Claude C4 must call `XgLifeHostActivity.SetReadingWindowVisible(windowOwner, visible)` on actual open/close/minimize changes and call false on disable. Destroyed owners are pruned; multiple reader owners are supported. Background `Sim.Reading` is deliberately not used.
2. No central player-pause API was found. Existing SaveSlotsPanel and disabled gameplay hosts already stop Controller updates. A future/actual pause UI must call `SetGamePaused(owner, bool)` and clear it on close/disable. Visual hit-stop (`Time.timeScale=0`) is not treated as a player pause. These exposed APIs do not mean absent callers have been integrated.
3. Windows native class/handle behavior, true OS minimize/restore, Alt-Tab without minimizing, and focus during full screen must still be verified in a standalone Windows player. Editor injected-state tests cannot prove native behavior. If detection is unavailable, the bridge logs a warning and does not invent a minimized state.
4. The authorized group coroutine lifetime repair has a prepared PlayMode check but has not run. Existing/future group-script sources must expose their actual running state; the property covers the current preset coroutine and group-bound script handler, not arbitrary prose arrival.
5. Current settings are in the existing title menu route. Persistence uses the loaded runtime and its save safety rules; real disk I/O and title preload/reload sequencing are not exercised by the new fixture.

## Verification entry (prepared, not run)

`LifeHostSettingsChecks.Run` requires both Core and host candidates. It covers injected minimized/restored states, ordinary long frames remaining active, hidden internal exe, explicit pause, multiple/destroyed reader owners, no-simulation disabled UI, one callback per mode change, reopening without overwriting mode, and Core JSON roundtrip/default. It uses direct Button callbacks. Real native window behavior, focus switching, physical input, bilingual rendered geometry, actual save I/O and authored group/reader flows remain unverified.

`LifeGroupPlaybackChecks.Frames()` is a prepared isolated PlayMode coroutine. It tests GameObject and component disable/re-enable, pending-queue preservation, old iterator finally versus a new generation, and cross-save discard before resume. It ends all test delivery coroutines before any rendered frame can send a message, restores Unity Random state, and does not instantiate ChapterOneRuntime. Normal full scripted dialogue delivery still requires integration coverage.

Dedicated runner: `LifeGroupPlaybackRunner.Run` via a separate batch Editor with `-executeMethod LifeGroupPlaybackRunner.Run` and **without `-quit`**. It opens an unsaved empty scene, installs/restores the Editor verification-save override, fails if a ChapterOneRuntime exists, watches runtime errors and a completion marker, has a 90-second timeout, waits two frames after fixture disposal, writes `%TEMP%/LingGuang-LifeGroupBatch-QA-<id>/report.json` / `passed.txt`, returns to EditMode and exits 0/1. It does not change Enter Play Mode options. It is prepared only, not executed.

Read-only validation performed: production SHA256 baselines unchanged, patch applicability with `--ignore-space-change`. No runtime PASS is claimed.
