# SFX background load isolated verification

Apply `sfx-background-load-checks.patch` to add two Editor-only files. Runner follows the existing registry runner pattern, no additional framework or production changes. Not compiled or executed by agent.

Dedicated Unity batch invocation: `-batchmode -forceAudio -projectPath <project> -executeMethod SfxBackgroundLoadRunner.Run -logFile <unique-log>` (NO `-quit`; runner exits after PlayMode). An available audio device is needed for actual isPlaying observations. Do not suppress audio or present missing-device failure as successful audio validation.

The runner starts an unsaved empty scene, observes existing enter-play-mode settings without changing them, isolates the verification save override, verifies absence of ChapterOneRuntime, runs the coroutine, and exits with 0 only on success. Outer timeout 90 seconds; resource Loaded wait 30 seconds. Output path is logged, with unique report directories under TEMP, preserving earlier evidence.

Checks:
- Exactly 58 WAVs under Resources/XgSfx, each reached through production Sfx.Clip/ComboClip; resource paths form the exact same set, excluding missing asset/procedural fallback substitution.
- Each real importer has background loading enabled; each AudioClip reports background and preload enabled.
- Production Init and ComboClip start loading; test only polls across natural frames (does not call LoadAudioData itself). Every clip must reach Loaded; Failed/unknown states fail immediately, unresolved Unloaded/Loading time out. Report includes initial/final per-clip states and observed wait durations.
- Actual Fan/Tick/Stamp/Fanfare plus Combo 1/10 Play calls must assign the expected source/clip and observe isPlaying. Audio-disabled settings fail explicitly.
- Does not unload or force a cold cache, does not simulate a load failure/retry, does not validate audible continuity. Warm import/editor caches may make all clips immediately Loaded. Timings are observed readiness, not listening, hardware latency or a performance comparison.

`git apply --check` passed. Root should compile and run after current UI checks as planned.
