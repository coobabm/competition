# Current Windows release smoke — preparation only

Supersedes the retired account/unsupported-option assumptions in `../release-smoke-plan-stage/RELEASE-SMOKE-CHECKLIST.md`. No Player, Unity, model or recorder was launched while preparing this handoff. Root alone builds and executes. One final binary, one Player, one deliberate short chat; no simultaneous Editor/native QA, packaging/model hashes, stress loop or extra model process. Aim for one bounded 10–15 minute run; stop on a reproducible blocker and preserve the run.

**Optional trace candidate now staged separately:** project `.coordination/integration-20261018/release-native-trace-stage/release-native-trace.patch` (current SHA28c06081...) adds metadata-only opt-in actual native lifecycle and YY provenance. It is not assumed imported or proven here. If root reviews/imports it before the final build, require `VERIFICATION_NATIVE_TRACE` and use `Summarize-NativeTrace.ps1 -TracePath <named-jsonl> -PlayerLog <this-log>` after the run. Its README supplies the same-ID proof chain and distinguishes `native_filtered` from unmodified native and fallback. Only successful actual trace evidence replaces the ordinary-Player provenance gap below. No trace evidence means retain unverified.

## Storage and preferences before launch

Use the actual final `build-result.json`/`source-provenance.json`. Confirm `result=Succeeded`, `errors=0`, ordinary Release (no RECORDING_PREVIEW), two title/Main scenes, and exact EXE path/hash. Do not use the historical build or old `RecordingPlayerSmoke`.

`--verification-save-dir` now exists in ordinary Player (`VerificationSavePaths.cs`). It redirects chapter data, DreamOS DesktopProfile, ModelCache and Music2016. First use must be a NEW/EMPTY absolute local directory, outside the build and all normal/legacy save roots, without links. Use two separate arguments, never `--verification-save-dir=...`. Runtime logs `VERIFICATION_SAVE_ROOT <absolute path>` and creates `.lingguang-verification-save` with header `LINGGUANG-VERIFICATION-SAVE:1`, that exact path, and a GUID. Invalid paths fail closed; never retry without isolation.

**PlayerPrefs is deliberately not redirected.** Even normal startup may write `LingGuang.Music...` defaults and `LingGuang.WindowSize.Handoff2`; title settings, language, help notes and tree zoom can also write. Before launch, with every game process stopped, use `PlayerPrefs-Guard.ps1 -Action Snapshot` for the final build's exact HKCU `Software/<company>/<product>` key. After exit, Compare then Restore using the recorded exited PID, then Compare again. Preserve the CLIXML backup. Guard handles an originally absent key, exact value types, and removes only values created during this run; it refuses product subkeys or mismatched build/SID. Do not run concurrent game sessions or restore while Player is alive. This is explicit restoration, not a claim that prefs were isolated or never temporarily changed. Snapshot/restore scripts are statically parsed only, not runtime tested.

Record before/after hashes of existing user save-shaped files (`chapter-one.sav*`, slot/profile/ending JSON) under actual company/product `LocalLow/.../LingGuangV05` and the known legacy `RippleLocalPreview/LingGuang-Playability-20261008/LingGuangV05`, `DefaultCompany/competition/LingGuangV05`. Read only; never move, reset or replace them. Exclude large embedding/model/media caches from this low-load hash pass and state that scope. No need to copy models or change application identity.

```powershell
# Replace FINAL_DIR once with the final build path, never a guessed 'latest' EXE.
$release = 'C:/ABSOLUTE/FINAL_DIR'
$tools = 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-smoke-current-stage'
$evidence = 'C:/Users/xomem/Documents/ChatGPT/games/LocalVerification/ReleaseSmoke/UNIQUE_RUN'
New-Item -ItemType Directory -Path $evidence -ErrorAction Stop | Out-Null
$save = Join-Path $evidence 'PlayerData' # Leave nonexistent; Player creates/marks it.
$exe = Join-Path $release 'MyComputerAlive.exe'
$build = Join-Path $release 'build-result.json'
Copy-Item -LiteralPath $build,(Join-Path $release 'source-provenance.json') -Destination $evidence
Get-FileHash -LiteralPath $exe -Algorithm SHA256 | ConvertTo-Json | Set-Content (Join-Path $evidence 'exe-hash.json')
& (Join-Path $tools 'PlayerPrefs-Guard.ps1') -Action Snapshot -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml')
$log = Join-Path $evidence 'player.log'
# This is the explicitly required interactive Player, not a background helper.
$player = Start-Process -FilePath $exe -WorkingDirectory $release -WindowStyle Normal -PassThru -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720','--verification-save-dir',('"'+$save+'"'),'-logFile',('"'+$log+'"'))
$pin = @{TargetProcessId=$player.Id;ExpectedExePath=$exe;ExpectedProcessStartUtc=$player.StartTime.ToUniversalTime().ToString('o')}
$window = Join-Path $tools 'Player-WindowQa.ps1'
& $window @pin -Action Inspect # Save actual handle/client dimensions; do not assume 1280x720 survived preferences/startup resizing.
```

Before any UI writes, confirm exact root log and marker. Existing fullscreen preferences can override the launch size; `PlayerWindowSize` may resize first boot. Read the actual window after loading. Use only observed coordinates, current HWND and pinned PID/start time. No blind coordinate loop, hierarchy invocation, save injection, hidden QA flags or fabricated setup.

## Fresh title → Main → saved setup

1. Capture actual title `01-title.png` after loading completes. The normal start control is the physical **case power button**. `Load game [L]`, `Endings`, and Quit are separate title actions. On a new empty verification directory, click power once; do not click Load or a completed archive. Existing completed saves route power to the ending archive, so they cannot certify fresh boot. Default camera flight is 4.8 seconds, but inherited reduced-motion preferences alter it; record the observed preference, not a fixed animation claim.
2. Wait through the scripted prologue while it owns the pointer. Player log checkpoints are `[Prologue] title`, `virus`, `txt`, `hijack`, `grab`, `shutdown`, `extract`, `setup`, `done`. At the visible shutdown question click **否 / No** once. Yes deliberately enters burn/repair and re-asks; Close is not an answer. Do not click scripted WinRAR extraction controls while the hijacked cursor acts.
3. At `[Prologue] setup`, after the prompt to open the exe and cursor release, open the newly installed AI exe icon through actual UI. Fill its visible setup form with a harmless test name (1–16 chars), displayed self/call options, and a short personality (1–60 chars). Click its actual completion button, wait for reading to finish, then answer the visible meeting Yes/No prompt. Record exact input and choices; don't bypass setup fields or write prologue/appInstall/stage values. Setup may itself submit one ordinary 80-token model parsing request with an 8-second UI fallback; this is normal game behavior, not the deliberate smoke chat below.
4. Require `[Prologue] done`, usable desktop, and an actual primary save beneath PlayerData. Capture `02-desktop.png`. Keep normal tutorial dialogue in order. Open Teach and Train only when the current UI permits; capture locked state honestly if still locked. Do not buy automation/train large workloads just to reach a later stage for smoke.

## One real chat and exact limits of inference evidence

Wait for normal local service readiness before the one deliberate short free-text message. Opening the title already preloads Main and its normal models. Do not launch curl servers, llama binaries, another Editor, another Player, `SerialNativeQa`, or a second GGUF to gather evidence.

- Exact success-at-boot signal: `[灵光] 本地模型就绪：<ModelName> · Cpu · <Architecture> · GPU 层 <...> · 3 slots × 4096 · 启动 <seconds>s`. Record the whole line, actual model name, CPU DLL/backend and start time. Failure signals include `本地模型启动失败`, `本地模型不可用，改用预设回复`, `本地模型请求失败`, `本地模型服务出错`, `模型回复出错`, `模型回复解析失败`, and `Cannot start native inference`. Collect full log, not only grep successes.
- Current Windows service is CPU, `--n-gpu-layers 0 --device none --fit off`, `--threads` AND `--threads-batch` = `max(1,min(6,Environment.ProcessorCount/2))`, q8 KV cache, 2048 MiB host cache. Default `parallelJudging=false`, three routed context slots (12288 total context). `XgBrain` requests `SetParallelJudging(false)`; setting true would request four slots. **Current `Pump()` has a global `active.Count > 0 || NativeCallsInFlight > 0` guard and returns after each dispatch**, so context-slot count is not concurrent completion count. This static rule is not Player runtime peak/cancellation measurement.
- Normal `LocalEmbedder.Attach` also starts its bundled embedding model, if present. It has one worker/service slot and 2048 context; its launch does not explicitly cap `--threads`, so do not copy the chat's six-thread bound to embeddings or claim aggregate process CPU is capped at six. We do not load any additional model beyond normal product startup. Wait for startup settling before audio/chat capture. If the strict requirement is zero simultaneous chat+embedding native work, the current Player has no cross-service measurement/control to prove it; record that limit.
- Use the real YY app and one available private conversation, preferably Qingwen for an unconstrained free reply; early-stage AI speech is deliberately constrained. Avoid netbar, quiz/known-trigger messages and repeated greetings, which have scripted branches. Example single request: `用一句话说说雨后窗外的样子。` Type into the observed field, send once, record sent UTC and complete returned text/UTC. Wait up to 75 seconds, no resend/retry storm. Any game-generated setup/background jobs remain ordinary behavior and are not counted as extra deliberate smoke requests.
- **No normal successful per-request native origin log currently exists.** `YYChatHub.Reply` can use offline fallback on null/repeated replies and can constrain native text. `LocalLlm.RunJob` logs failures but no successful request/result lifecycle. The native owned log `qaLog` is only for `editorQaInstance`; runtime logging drops to level1. Readiness + a plausible bubble proves responsive UI, not that this exact bubble is native. Save `ui_response_observed` and `native_origin=unverified` unless the final source/binary has separately reviewed exact successful native callback evidence. Do not pretend the hidden QA flags or historical empty-scene Editor result are natural Player UI. Exact cancellation/next-dispatch ordering is likewise unverified in this smoke.

The adapted window helper supports `TypeText` and `Key` (Enter/Escape/Tab/Space), always rechecking exact foreground HWND/PID; it uses SendInput Unicode, no clipboard. These additions are statically parsed, not executed here. First click the observed text field. Save before/after screenshot and each helper JSON result. If input fails, stop and use the human operator; no direct message injection.

```powershell
# $hwnd must be the actual Inspect result. Capture never falls back to the desktop.
& $window @pin -WindowHandle $hwnd -Action Focus
& $window @pin -WindowHandle $hwnd -Action Capture -OutputPath (Join-Path $evidence '05-yy-before.png')
# Click the CURRENT observed input coordinates first, then type once.
& $window @pin -WindowHandle $hwnd -Action TypeText -Text '用一句话说说雨后窗外的样子。'
# Click the visible Send button; Enter is allowed only after observing that it sends rather than adds a newline.
```

## Music sequence and real output continuity

Use bundled library tracks; `2016 热歌` lists unbundled copyrighted songs and cannot prove playback on an empty isolated Music2016 directory. No user music is copied or downloaded. `GameMusic.OnMusicOpened` on the first real open each boot forces repeat=false, shuffle=false and starts clip **emergency_1** regardless of alphabetical list position. Record its displayed title; do not assume that is row1. It fades in and speaks a short opening line, which may duck music normally.

1. Before music app open, record 3 seconds baseline if needed (title/prologue/fan sounds are not guaranteed silence). Actually open music from desktop; wait for initial speech/fade, capture current title, visible list ordering, repeat/shuffle states and two advancing progress times. One 12–15 second loopback take establishes expected audible music only over that interval.
2. Start a 15-second take, click the native music **Close** during the take, continue listening, and reopen through its real app icon. Capture same title and advanced elapsed time. Closed-window music is deliberately owned by resident `GameMusic`; do not Pause or Stop. Audible continuity in the WAV plus title/progress proves this interval. Window hidden alone or a clock advancing alone does not.
3. For a bounded **sequential next-track boundary**, choose a bundled visible track with a visible successor. Keep shuffle and repeat off, use the actual progress slider to seek about 5 seconds before its end, record seek UTC and title/order, then stop touching controls. Record 15–20 seconds spanning natural end. Require the next eligible displayed track (wrap only at end), its timer restart, and expected new audio. Label as **user-seek-assisted natural boundary**, not full-track listening, daily song completion, or no-seek playback. If seek is not exposed, use actual Next once to check manual ordering and mark natural auto-advance untested; don't invoke NextTrack by script. Close/reopen should retain the chosen mode and ongoing session. A new boot resets to sequential by design.
4. Optionally let the same music run during the one chat and take 20–30 seconds around sending/response. Do not start another inference to chase a timing window. State exactly which interval overlapped the real request; a capture shorter than response latency does not cover the whole request.

Reuse `record_loopback.py` unchanged from release-audit-stage (same SHA256), with its existing isolated audio-env Python. `--inspect` does not record; only `--record` does. Each take is 1–30 seconds; watchdog = duration+15 seconds, new absolute WAV only. No microphone fallback. Run background capture with `Start-Process -WindowStyle Hidden` if clicks must occur during capture; allow it to finish, check exit code, `.json` completed=true and no `.failed.json`, and preserve originals.

```powershell
$audioPython = 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-audit-stage/audio-env/Scripts/python.exe'
$audioScript = Join-Path $tools 'record_loopback.py'
& $audioPython $audioScript --inspect | Set-Content (Join-Path $evidence 'audio-device.json')
# Execute only when the real take is intended. Quote absolute paths in Start-Process ArgumentList.
$take = Start-Process -FilePath $audioPython -WindowStyle Hidden -PassThru -ArgumentList @(('"'+$audioScript+'"'),'--record','--seconds','15','--output',('"'+(Join-Path $evidence 'music-close.wav')+'"')) -RedirectStandardOutput (Join-Path $evidence 'music-close.stdout.txt') -RedirectStandardError (Join-Path $evidence 'music-close.stderr.txt')
# Record close/reopen UTC and real helper action outputs during this finite take, then inspect its result.
```

Loopback includes all apps on the default output. Quiet unrelated audio. Record endpoint id/name, actual WAV+JSON SHA, timestamps and action timeline. **Listen to the WAV** for correct tune, clicks, dropouts or restart. RMS/peak/nonzero and 100ms blocks are supporting evidence only. Silence >=300ms at -60dBFS is a candidate event; compare with intro speech ducking, deliberate seek/track gap, pause, shutdown fade and other apps before diagnosing a dropout. No signal metric alone gives an audio PASS.

## Finish and bounded evidence

Exit through the actual game shutdown/title flow, capture returned title, then Quit. Wait for process exit; collect exit code and complete player.log. Do not kill a model worker separately or run packaging while native cleanup is still active. If it hangs, record failure and let root decide process handling.

```powershell
# Only after the recorded Player has exited:
& (Join-Path $tools 'PlayerPrefs-Guard.ps1') -Action Compare -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml')
& (Join-Path $tools 'PlayerPrefs-Guard.ps1') -Action Restore -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml') -ExitedPlayerProcessId $player.Id
& (Join-Path $tools 'PlayerPrefs-Guard.ps1') -Action Compare -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml')
```

Compare normal user-save hashes with baseline. Preserve isolated PlayerData, prefs backup, build provenance, helper outputs, screenshots, logs and WAVs. Fill `evidence-template.json` per claim. Do not turn source inspection, package/build success, native readiness, or historical Editor results into actual Player inference/cancellation/audio claims. This preparation does not unblock the existing missing per-request provenance; it makes that limit visible while allowing the genuine title/Main/UI/music smoke to run.
