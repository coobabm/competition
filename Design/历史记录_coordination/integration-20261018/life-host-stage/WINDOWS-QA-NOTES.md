# Native Windows minimization verification: existing infrastructure

Read-only inspection; no build or player was started.

## Existing entries

- `Assets/LocalBuild/Editor/IntegratedReleaseBuild.cs`, `IntegratedReleaseBuild.Run`: full title/Main build, StandaloneWindows64, Mono2x, Direct3D11, graphicsJobs=false. Captures source provenance and restores changed player settings in finally. Uses the production company/product save identity, so a resulting ordinary launch is **not an isolated QA launch**.
- `Assets/LocalBuild/Editor/RecordingPlayerBuild.cs`, `RecordingPlayerBuild.Run`: full title/Main build, separate company/product, `RECORDING_PREVIEW` extra define. Existing `ChapterOneRuntime.EnsureInitialized` then redirects saves beside the player into UserData; `--recording-smoke` uses VerificationData. Existing `RecordingPlayerSmoke` also redirects DreamOS profile storage. This route runs unrelated stage-4 checks and is larger than necessary for a native-window probe.
- `WindowsReleaseBuild.Build` and `LatestDownloadedBuild.Run` pin D3D11/Mono but contain old fixed output/version values and do not restore every changed setting. Do not reuse those verbatim for a small new QA run.
- `ProjectSettings/ProjectSettings.asset` has `runInBackground: 1`. The new detector must be validated in an actual Windows player; Editor-probe results cannot exercise its `UNITY_STANDALONE_WIN && !UNITY_EDITOR` branch.
- `ChapterOneRuntime.VerificationSaveDirectory` is **UNITY_EDITOR-only**. A standalone player cannot be isolated by relying on this Editor field.

## Smallest next implementation proposal (not yet implemented)

Create a QA-only `NativeLifeWindowBuild.Run` using the graphics/backend restoration pattern from IntegratedReleaseBuild. Generate a separate empty verification scene with just a native activity probe, build it to a unique LocalVerification/NativeLifeWindow/<stamp> directory, and use a dedicated extra define for the probe. Do not include Main or instantiate ChapterOneRuntime. This tests native HWND/PID/class/IsIconic and the host ActiveClock while avoiding player saves, model loading and full game setup. Preserve/restore editor scene setup and all touched PlayerSettings.

Probe/report should record the current process ID and own Unity window handle, IsIconic, Application.isFocused (diagnostic only), LifeEventActive, lifeNext/lifeClock deltas and observed recovery frame. Test a normal focused window, a still-visible unfocused window, native minimize then restore, an ordinary long frame, and hidden internal exe semantics through the separate host test. If automating ShowWindow, target only the previously verified own HWND; have a bounded restore timeout and never use GetForegroundWindow to discover the game. Alt-Tab/another app focus remains a distinct acceptance step from minimize.

For complete gameplay integration after that minimal probe, derive a separate title/Main QA build with RECORDING_PREVIEW isolation (including DreamOS profile path), a unique output and no hard-coded recording-stage assertions. Do not label the small empty-scene native probe as whole-game acceptance.
