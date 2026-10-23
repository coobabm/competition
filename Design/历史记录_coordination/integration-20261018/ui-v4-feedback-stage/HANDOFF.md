# UI v4 feedback staging

Production patch: feedback.patch (XgButtonPressFeedback.cs, XgJuice.cs only).
- Existing visual label/graphic target holds at 0.95 on left down. Release rebounds for 0.22 seconds with a single small overshoot and exact rest.
- Keyboard submits press + release before business callback. Pointer exit/cancel/disable/hidden host clear immediately. No business click is manufactured.
- Reduced host skips new feedback and clears active private feedback on the next LateUpdate. Host business FX are untouched.
- Existing hit rectangle, cable anchor and business Knock remain unchanged. This batch does not scale the whole button shell; that requires a separate visual wrapper/layout migration.

Validation: git apply --check passed. Unity Roslyn full Assembly-CSharp response file compiled the staged sources and UiV4PressChecks.cs successfully (exit 0, existing warnings). Compiler outputs remain in this staging directory. Unity PlayMode has not been run by this agent.

PlayMode: copy UiV4PressChecks.cs into Assets/LocalBuild/Editor, then yield return UiV4PressChecks.Frames() from the existing shared PlayMode runner. It checks immediate 0.95, long hold, repeated press, stable hit rect, release/rest, Reduced toggle, keyboard and cancellation. Expected log UI_V4_PRESS_CHECKS_PASS. Also rerun WiringMotionChecks.Frames(actualCjkFont), already driven by DesignWallpaperSmoke.Wiring.cs. Existing WiringMotionChecks releases before normal WaitRest, so no change is needed there; its Reduced <=0.015 acceptance includes the new zero motion.

## Final opt-in revision
V4Press defaults false: parent must set button.V4Press = dark in XgUi.Button. Legacy shallow/light applications retain the original Knock timing, reduced amplitude and settling. New UiV4PressChecks enables V4Press explicitly.
XgJuice.DisableMotionWhenReduced defaults false: parent enables it for the .exe root Juice. While Reduced it blocks Flash/Shake/HitStop/Knock/Float/Burst/TransferCoins/Crumble/Shockwave/FlashWhite/Trail. Update clears an active animation stack on toggle; Play/PlayClip/PlayCombo remain independent and unchanged. Set this opt-in on child effect stacks where the root Reduced setting is propagated. No global shallow-app behavior changed.
Latest compile.log: exit 0 for production staged changes plus UiV4PressChecks; git apply --check passed again. Unity test has not run. The exact existing regression entry is DesignWallpaperSmoke.RunWiringMotion (PlayMode natural frame runner, no -quit); new UiV4PressChecks.Frames() must be yielded from a PlayMode runner, not called as executeMethod.
