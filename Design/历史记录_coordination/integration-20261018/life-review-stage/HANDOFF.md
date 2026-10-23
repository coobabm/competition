# Life result Main UI QA candidate

Only add `candidate/Assets/LocalBuild/Editor/LifeResultUiPlayQa.cs` to the production tree for parent-controlled serial Unity compilation. No runtime production edits accompany this candidate. Requires the integrated cadence core, result card, and host work.

Run `LifeResultUiPlayQa.Run` in a separate Editor process with `-executeMethod LifeResultUiPlayQa.Run` and without `-quit`. It enters Main PlayMode and exits 0/1 in batch. Output: `LocalVerification/LifeResultUI/<UTC>/result.json`, optional `passed.txt`, and LCD PNGs.

Isolation is installed BeforeSceneLoad: `ChapterOneRuntime.VerificationSaveDirectory` points at that run's Save folder and DreamOS dataPath points at its DesktopProfile folder. The fixture refuses to proceed unless runtime.SavePath is inside that exact isolated Save folder. LLM is disabled and prologue/story drivers stopped. Main's bundled font and actual view are used. Waits between queued checks allow real rendering before raycasts or glyph assertions.

`SetSimulationForTests` sets TestMode=true and disables disk saving, which makes XingGuangHost refuse money operations. This fixture deliberately restores its private TestMode setter to false after verifying isolation, restores useDiskSave=true, keeps runtime.tickEnabled=false and controller.enabled=false, and invokes the real BindSave. The original isolated AtomicSaveStore remains in use. This is test code only; it is not a production workaround.

Loan force-open is deliberate deterministic setup, not a scheduler-eligibility assertion. The real event chooses 5000 principal from 100000 wallet. The button callback must write wallet and result into the same checksum-valid primary atomic save. Reload is from AtomicSaveStore.Load payload through JsonUtility/GameState, then normal controller BindSave. Old card callbacks are invoked immediately, before its next Update, to cover stale save binding.

The final confirmation uses an in-memory 73-second sentinel to prove it resets to480; then a111 sentinel proves duplicate hidden callback does not reset again. Controller ticking is frozen throughout, and no repayment time elapses.

Prepared only: no compile, PlayMode, native minimization or full process restart was executed by the preparing agent. Parent should review actual PNGs and report failures verbatim. A narrow Editor SearchDatabase infrastructure exception is separated only when its stack has no Assets frame; other errors fail the run.
