# Rapid minimize/restore candidate

Production change: one call in WindowManager.OpenWindow, `Play("In")` -> `Play("In", 0, 0f)`. Authored animation remains responsible for alpha/interaction/raycast/scale. Original Fullscreen/Normalize guard, window geometry, flags, taskbar callbacks and coroutine durations remain unchanged. No QA state forcing.

Evidence from SidebarLayout/20261018-131434-767:
- AI Files: isOn true, active true, alpha0, blocks/interact false, scale.01, animator disabled, normalized1.092244, fullPathHash -483381067.
- Offline CRC32 for Base Layer.Minimize equals -483381067; Base Layer.In equals -436882359 (the training window hash in the same report). Runtime state-name confirmation can be added if needed; no Unity execution was performed here.
- Source sequence is MinimizeWindow then Show/OpenWindow without a rendered animator evaluation between them. Animator.Play default normalized time is negative infinity; when it still reports the previous In state it can preserve time/state instead of overriding pending Minimize. Explicit0 requests the entry clip from its beginning.
- Window.controller Minimize uses Window_Out.anim (GUID076606e87a7a90c4195b4c0467c57f73); this animation has no animation events. The apparent delayed hide is animated CanvasGroup state, not an AnimationEvent callback.
- OpenWindow already stops the named DisableAnimator coroutine then starts a replacement with disableObject=false. Therefore an old close coroutine surviving normal Open completion is not supported by this code path. The replacement can finish and disable the animator on the wrong state; the captured normalized>1 disabled Minimize is consistent.

Validation needed: original FilesWindowUiChecks exact rapid minimize/Show sequence (keep timeout/raycast diagnostics), normal first-open/close/reopen, taskbar restore, completed minimize restore, and already-fullscreen/normalize focus must retain sizing/flags. Current narrow change deliberately preserves the fullscreen guard; it is not a broad redesign of every possible fullscreen/interrupted-animation combination.

Apply-check passed. Not compiled, not run, no passing claim. This is the minimal cause-directed candidate pending root's live verification.
