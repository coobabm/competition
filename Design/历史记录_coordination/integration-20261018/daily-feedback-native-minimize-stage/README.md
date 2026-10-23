# Daily feedback: native minimize visibility

Three-file staged candidate. Production and historical evidence were not edited. No Unity/Player, C#/.NET compilation/tests, model or audio was run.

## Evidence and cause

`LocalVerification/DailyFeedbackActual/20261018-232507-685/report.json` remains FAILED at `native Minimize hides lab`: 191 assertions, empty errors/cleanupErrors, fixed 1280x720. Actual guide Fold and native Minimize input hit passed. The failure full-frame shows the native animation in progress; that immediate visual observation alone does not establish its terminal state.

`WindowManager.MinimizeWindow` intentionally retains `isOn=true` and the active GameObject. `CloseWindow`, in contrast, clears isOn and later deactivates. `Window.controller` maps Minimize to GUID `076606e87a7a90c4195b4c0467c57f73`, `Window_Out.anim`; its root CanvasGroup alpha, interactable and blocksRaycasts reach zero at 0.166667 seconds, scale ends at 0.25 seconds. `DisableAnimator(false)` retains the object. Waiting alone cannot make the old `XingGuangView.Visible` false because it tests only isOn and active/enabled.

The already-passing public archive and completed replay checks assert their own presenters/session are dismissed by `XgEpilogueMailBridge.NativeMailboxDismissed` subscribed to onMinimize. They do not assert Window.isOn=false. `FilesWindowUiChecks.Restored` likewise uses CanvasGroup input/opacity and animation completion to establish an actually restored native window.

## Candidate

- `XingGuangView`: track native onMinimize/onOpen for presentation visibility, preserving native isOn semantics and background simulation. Minimize immediately suppresses Visible and clears transient toast. Open clears the flag. Added listeners are removed on destruction.
- `XgTrainPage.Rhythm`: a one-line internal cancellation method clears pending rhythm on minimize. This is needed because the corrected Visible makes the view's Update return before TickRhythm; relying on that tick to cancel a previously pending cue would leave a stale cue during a quick restore. No synthetic epoch, result, audio or cue is introduced.
- `DailyFeedbackActualUiChecks`: keep the real LCD Train/Minimize hit path; immediately require presentation suppression and no pending cue. Then wait at most five seconds for the independent native endpoint: isOn remains true, object stays active, Animator state is Minimize and settled, root CanvasGroup alpha <=0.001 and both input flags false. Capture a new minimized screenshot. Only then re-enable the real controller and await natural epoch completion. Preserve exact one-round/no-cue assertions, and additionally require opacity/input/Visible restored after native reopening. Existing purchase, final disk, reduced-effects, Fold and fixed GameView checks remain intact.

This separates synchronous presentation cancellation from asynchronous actual visual hiding. It does not replace visual evidence with the new flag or force any CanvasGroup/animator/input state. No WindowManager, clock, lesson fixture, economy or Sim.WindowOpen change is included.

## Static validation and pending runtime

`package.py` checks lexical delimiter balance and production drift and emits the patch/manifest. `git apply --check` passed against current production. `failure-audit.json` records the original failed run and SHA256 for each evidence file, without changing its status.

The serial owner must import and rerun DailyFeedbackActual. Required new evidence: Minimize true input hit, real endpoint screenshot, immediate suppression, exactly one naturally completed hidden epoch with unchanged lastRhythmText/no pending cue, restored visible input with no stale cue, reduced-effects result, final real-save purchase/epoch checks, and clean GameView restoration. An already-pending audible accent cancellation race is statically covered by explicit cancellation but is not independently runtime-certified by this fixture. No new runtime pass is claimed.
