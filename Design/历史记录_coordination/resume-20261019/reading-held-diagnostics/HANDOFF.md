# Paused for Claude handoff

Parent requested stopping PublicReading development on 2026-10-19 so Claude can own the remaining work. No production edits and no Unity run by this agent.

## Current candidate (generated, not validated)

`reading-held-diagnostics.patch`, SHA256 `a2af7313cf9838370207d87262b71080ea014f41ac3f4014cd48da0683d98ca1`.

`manifest.json` records exact baseline/candidate hashes. `baseline/` and `candidate/` contain the source snapshots. `prepare.py` regenerates the candidate from current production; do not rerun after production changes without reviewing/rebasing. `prepare_compile.py` was copied from the prior ending runner compile preparation and has NOT been run here.

Candidate changes only QA sources: existing `PublicReadingInteractionUiChecks.cs`, new `PublicReadingHeldPointerProbe.cs` and meta. Adds bounded first-eight/last-sixteen observations during the failing outside-notice held-pointer interval. Captures input-before/after fixture maintenance, early player Update, LateUpdate, and final editor observation: mouse IDs/raw/pressed, real InputBusy result, notifier enabled, TestMode, focus/minimized, save/lab/delivery identity, count/safe delta/quiet and delta time. Logs the trace and includes it in the existing three assertion failure messages. Original assertion Boolean expressions and input maintenance behavior are retained. Probe brackets the Update phase, not the exact notifier invocation. Sampling is read-only but can affect timing; passing with instrumentation alone does not prove a fix.

## Proven evidence

Production report `LocalVerification/SidebarLayout/20261019-093453-647/report.json` passed 26 bilingual photo checks and the held synthetic mouse/current-device and >=3 player-frame assertions, then failed the strict L319 count/safe/quiet assertion. Report has no actual count/safe/quiet values, so cause remains unproven.

`EligibleFixture` installs initialized stage3 state at safeSeconds=600, stageSafeSeconds=60, quietRemaining=0 and an immediately eligible record. Held condition in notifier must reset quiet to30 and earn no safe time. Production notifier reads InputBusy each Update. The QA iterator advances on EditorApplication.update, which can read a different input buffer from the player. Existing HeldMouseFixture writes Dynamic+Editor buffers and restores Mouse.current in player onAfterUpdate; final editor pressed=true is insufficient evidence of each notifier Update state.

InputSystem package `com.unity.inputsystem@21a28c3a6c83`: InputManager.UpdateState compares/monitors through the currently active buffer, then writes the explicit updateType buffer; ButtonControl.isPressed reads either value or cached editor/player press flag depending on needsToCheckFramePress. This suggests a possible buffer/cache issue but is NOT a diagnosis. Do not change production or weaken assertions on that speculation.

## Remaining work for Claude

1. Review candidate/patch for syntax, probe lifetime and whether diagnostics are sufficient. Check static execution-order overrides if relying on early Update ordering.
2. Run `git apply --check` read-only against current production; this candidate has NOT had applicability checked yet.
3. Run isolated C# compilation if appropriate. No compile, test or Unity pass is claimed for this candidate.
4. Have parent integrate an approved candidate and run Unity serially. Do not launch a concurrent Unity instance.
5. Use actual samples to distinguish transient input loss, notifier not running/binding mismatch, or genuine production scheduling behavior. Preserve exact count0, safe equality, quiet30 and >=3 player frames.
6. After a proven cause, prepare the smallest independent fix and rerun full photo/input/tray/natural quiet30/LCD pointer-up/life-priority acceptance through parent.

Separate completed YYGirlfriend ending-runner one-line patch is in `../../claude-batch-01/ending-small-A-ui-girlfriend-followup/` and already handed to parent. It has its own README/manifest and passed applicability when generated.
