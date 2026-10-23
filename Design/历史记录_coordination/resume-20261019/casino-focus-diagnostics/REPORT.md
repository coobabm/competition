# Casino focus review: cause remains unproven

Only the three named reports, corresponding logs and the relevant focus/save/bind paths were inspected. No production or Claude D1/D2/D3 file was changed. No Unity/model/test/compiler was launched; parent owns the current serial C1 run.

| Run | Verified failure location | Evidence boundary |
|---|---|---|
| 20261019-100341-033 | `zh-paid`, 41-second gossip post-load dedup wait | Last successful check is exact restored public message/dedup receipt; wait completion absent. |
| 20261019-100826-276 | Initial Foreground 60-second stabilization deadline | Only fixed-resolution check completed; no casino outcome or gossip ran. |
| 20261019-102622-188 | `en-paid`, 41-second gossip post-load dedup wait | Entire Chinese path, including its full41-second no-repeat check and frozen blackjack, already completed. English paid message and restored receipt passed before interruption. |

All three reports have empty runtime errors and cleanup errors. Existing failure text does not distinguish isFocused=false from minimized=true, or identify elapsed time inside WaitSeconds. Report order establishes the interval, not the moment or cause of focus loss. Whole GameView screenshots do not establish which desktop/editor window owned focus.

Source checks:

- Runner only calls Editor GameView Show/Focus during initial Run. Tick calls Repaint, which is not an explicit Focus request. No Editor pause or native minimize request appears in SaveSlot/Bind/LoadSlot.
- SaveSlot and LoadArchive use synchronous isolated persistence and Changed/Saving events. Hub and controller rebinding changes in-game state/UI. No direct Editor focus/pause/native minimize call was found on the reviewed path. This does not prove that all editor/package/OS callbacks are irrelevant.
- DreamOS `WindowManager.FocusToWindow()` only calls Transform.SetAsLastSibling; it is in-game canvas order, not OS or Editor focus.
- `PlayerWindowSize` native resize/title code is excluded in UNITY_EDITOR. `PlayerWindowState.IsMinimized` in Editor only reads EditorMinimizedProbe, reset to null at subsystem registration. Casino does not assign it; only the separate LifeHostSettingsChecks writes/restores it. Existing reports omit its value, so do not retrospectively assume it was null.
- All three logs include the AI Toolkit account-access warning and licensing404 messages. Neither records a matching focus transition/current window nor establishes causation. Do not attribute this to the user, licensing UI, editor packages or the environment without further evidence.
- The successful Chinese41-second interval in102622 argues against an unconditional save/reload-induced failure. The two failures after the same kind of roundtrip justify observing that boundary next, not changing its behavior.

## Minimal diagnostics candidate

`casino-focus-diagnostics.patch` changes only the Editor QA runner. It labels each0.5s/41s wait and the three stabilization sites. Failure details include phase/language/UTC/elapsed/frame/frameDelta/isFocused/minimized/probe-presence/editorPaused/playing/focusedEditorWindow type. Report `focusObservations` holds bounded-by-run snapshots immediately before and after gossip SaveSlot, Bind and LoadSlot, and each timed wait's begin/end. Snapshot reads do not focus a window or mutate pause/input/save gates.

The 60s stabilization deadline, continuous3s foreground requirement, existing per-frame `focused && !minimized` predicate,0.5s/41s real-time deadlines and gossip40-second schedule are unchanged. No resumed timer, skipped background duration, retry or autofocus is added. Exact messages/receipts/snapshot assertions remain unchanged. No screenshot/file I/O is added per frame.

Limitation: Tick only advances on new player frames, as before. These diagnostics cannot observe an entire pause interval while no frame advances, nor identify another application's focus owner. They do distinguish sampled editor state and whether a focus change was already visible at the synchronous roundtrip boundaries. A focus change after a boundary remains unattributed until actual evidence supports it.

- Patch SHA256: `d7c907b98376daeacc2f6a53db51c7484fd126ad4502fe75351115bf01db7d6a`
- Independent baseline/candidate and exact hashes: `manifest.json`.
- Production `git apply --check` passed read-only. Compile/Unity not run.
- Parent integration/test entry, after the current Editor exits: **CasinoScamActualUiRunner.Run**, same real foreground serial execution. Do not apply while C1 is running.

Next interpretation: a changed snapshot immediately after one synchronous call narrows the suspect boundary; false at wait entry means the loss preceded the wait; true entry with later false narrows it to the observed elapsed interval. None of these alone identifies who caused the change. Preserve the strict failure and actual timing.
