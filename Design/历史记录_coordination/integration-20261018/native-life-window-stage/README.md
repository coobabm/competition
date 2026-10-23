# Native life-window probe candidate

Prepared only. No Unity, compiler, player or native window operation was run while preparing these files.

Apply `new-files.patch` after the Core/host cadence integration. It adds two C# files and their metas only. The PowerShell runner stays beside this document outside Assets.

Build in a dedicated batch Editor with `-batchmode -quit -projectPath <project> -executeMethod NativeLifeWindowBuild.Run -logFile <build-log>`. The entry creates a uniquely named empty verification scene with one probe and camera, builds Windows64 Mono/DX11 with `NATIVE_LIFE_WINDOW_QA`, restores modified PlayerSettings and scene setup, and deletes only its generated scene asset. It leaves `runInBackground` unchanged so the test detects a bad production setting. Output is `%TEMP%/LingGuang-NativeLifeBuild-<guid>/`; the exact exe is also in `LocalVerification/NativeLifeWindow/latest-build.json`.

Run in an interactive Windows session:

```powershell
& '<stage>/Run-NativeLifeWindowProbe.ps1' -Executable '<temp-build>/NativeLifeWindowProbe.exe'
# Optional true keyboard Alt-Tab observation, requiring the operator to press it:
& '<stage>/Run-NativeLifeWindowProbe.ps1' -Executable '<temp-build>/NativeLifeWindowProbe.exe' -ManualAltTab
```

The script starts hidden, enumerates only the new process's HWNDs, verifies executable path, owning PID, IsWindow and UnityWndClass immediately before each ShowWindow/SetForegroundWindow operation, then shows that window. It never changes another HWND and never sends global keys. Windows may deny focus activation; inability to observe required states fails instead of being reported as a pass. Default verification obtains unfocused-visible state using SW_SHOWNOACTIVATE after minimizing the probe. This proves the state relevant to Alt-Tab without falsely claiming a keyboard Alt-Tab was performed; `-ManualAltTab` adds the actual operator-driven path.

Each run uses `%TEMP%/LingGuang-NativeLifeRun-<guid>/` for `identity.json`, `frames.jsonl`, `complete.json`, `Player.log`, and `assertion-report.json`. A unique `LingGuangLocalVerification/NativeLifeWindowQA-<build-guid>` company/product isolates persistentDataPath and PlayerPrefs from the game. This is necessary because existing global PlayerWindowSize initialization writes a window-size preference even in an empty scene. No ChapterOneRuntime or game scene is created; the probe constructs only an in-memory XgSim and never calls persistence. The report records the isolated persistent path; no user save path is inspected or modified.

Assertions cover actual native IsIconic versus PlayerWindowState, minimized frames inactive, first restore frame inactive and next frame active, focused and visible-unfocused frames active, and an ordinary 1.2-second frame staying active. Focus is a recorded diagnostic only. The displayed accumulator is a harness measurement, not Core's full gameplay cooldown. Native state races are preserved in raw data; a missing clean recovery transition fails rather than fabricating evidence. Player timeout is 100 seconds; script waits are bounded, and finally restores/quits only its own probe (kills only that launched process if unresponsive).

The runtime component exists only under UNITY_EDITOR or NATIVE_LIFE_WINDOW_QA and has no runtime-initialize hook. Normal player compilation excludes it; generated verification scene is not added to Build Settings. This is a small scene in the existing project, not a new stripped Unity project: project Resources/StreamingAssets and global initializers may still be included, so build size/time is not guaranteed tiny. It is not whole-game, save/persistence, full-screen, or event-result UX acceptance.
