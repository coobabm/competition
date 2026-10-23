# Current Windows release smoke — source consistency re-audit, preparation only

Supersedes the retired account/unsupported-option assumptions in `../release-smoke-plan-stage/RELEASE-SMOKE-CHECKLIST.md`. No Player, Unity, model or recorder was launched while preparing this handoff. Root alone builds and executes. One final binary, one Player, one deliberate short chat; no simultaneous Editor/native QA, packaging/model hashes, stress loop or extra model process. Aim for one bounded 10–15 minute run; stop on a reproducible blocker and preserve the run.

**Trace is now imported in the project source.** `ReleaseVerificationTrace.cs`, `LocalLlm.cs` and `YYChatHub.cs` emit metadata-only request lifecycle/provenance whenever the existing `--verification-save-dir` resolves successfully. There is no additional `--verification-native` switch. The trace is not enabled for ordinary unisolated startup. Source presence is not proof that a historical binary contains it: build the reviewed source, confirm `VERIFICATION_NATIVE_TRACE <exact-jsonl-path>` in this Player's log, and preserve the exact file. `Summarize-NativeTrace.ps1` is already present in release-smoke-current-stage. Native-origin and cancellation conclusions remain conditional on the actual run, as specified below.

## Pin the build, packages and distributed notices

Read-only audit date: 2026-10-19. Project Unity is **6000.4.5f1 (cc83ebd631f8)**. `IntegratedReleaseBuild.Run` sets build version **2026.10.19.1** for the build and restores the prior project setting afterwards (currently 2026.10.12.2). It emits `Build/MyComputerAlive-2026.10.19.1-<UTC yyyyMMdd-HHmmss>/MyComputerAlive.exe`, with `MyComputerAlive_Data`, `build-result.json` and `source-provenance.json`; also updates `LocalVerification/Release/latest-build.json`. It uses Windows x64, Mono, Direct3D11, graphicsJobs=false, BuildOptions.None and exactly ComputerCaseMenu/Main. Current source has no Standalone RECORDING_PREVIEW define. Do not use old `WindowsReleaseBuild.Build`, which writes an older version and changes company/product. Integrated build preserves the current identity, currently **DefaultCompany / competition**; always use the build-result values for PlayerPrefs and saves.

Package pins read from manifest + lock: LLMUnity **3.0.3**, git `9931a9ea46906b1f43adaf9498c05ccf227b5d0b`; LlamaLib **v2.0.5** from LLMUnitySetup; MCP `30d22075093d1d35dfb0091c1c7550e9ad948577`; Aura `558ea3c324c2cfd2803f3486bedbe9cbb740df9d`; Input System **1.19.0**, URP **17.4.0**, Newtonsoft **3.2.2**, Test Framework **1.6.0**, uGUI **2.0.0**. Preserve manifest/lock in source provenance; these are source pins, not proof all Editor packages are shipped runtime dependencies.

Only root executes the following after serial UI acceptance and stopping other project Editor/Player/native workloads. These commands were inspected, not run by this audit:

```powershell
$project = 'C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016'
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.4.5f1/Editor/Unity.exe'
$buildLog = 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/UNIQUE-release-build.log'
$buildProcess = Start-Process -FilePath $unity -WindowStyle Hidden -Wait -PassThru -ArgumentList @('-batchmode','-quit','-projectPath',('"'+$project+'"'),'-executeMethod','IntegratedReleaseBuild.Run','-logFile',('"'+$buildLog+'"'))
if ($buildProcess.ExitCode -ne 0) { throw 'Build process failed; inspect full build log' }
$buildRecord = Get-Content -LiteralPath (Join-Path $project 'LocalVerification/Release/latest-build.json') -Raw | ConvertFrom-Json
$release = $buildRecord.path
if ($buildRecord.result -ne 'Succeeded' -or $buildRecord.errors -ne 0 -or $buildRecord.version -ne '2026.10.19.1') { throw 'Unexpected final build result' }
```

Require a current `INTEGRATED_RELEASE_BUILD_PASS` line naming that exact directory and record the build UTC; a stale latest-build pointer is not evidence. `source-provenance.json` includes dirty/untracked working source but excludes GGUF and files >16 MiB; do not call it a complete payload hash.

The only source StreamingAssets GGUF observed is `LingGuang/Models/Qwen3-4B-Q4_K_M.gguf`, 2,497,280,256 bytes. Existing checksum file says `7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5`; this audit did not rehash 2.5 GB during concurrent UI acceptance. Model LICENSE is Apache-2.0, Copyright 2025 Alibaba Cloud; official Qwen model card is present. The final packager hashes the built model serially, after Player/native exit. Current runtime files are five CPU DLLs (avx/avx2/avx512/noavx/runtime) under `LlamaLib-v2.0.5/win-x64/native`. No embedding GGUF is present.

A raw Unity build is not the complete legal-notice package. `Package-VerifiedRelease-LowLoad.ps1` copies the build into `OUTPUT/payload/<build-folder>`, adds `ThirdPartyNotices`, and creates `MyComputerAlive-Windows.7z.001` etc. Required notices include LLMUnity Apache-2.0 license and upstream Third Party Notices, USearch Apache-2.0, actual LlamaLib v2.0.5 Apache-2.0, pinned llama.cpp/jsonhpp/cpp-httplib MIT and FeatureDetector CC0; model LICENSE/MODEL_CARD remain beside GGUF. Its authentic notice input is `release-audit-stage/native-licenses` with recorded upstream tag/gitlink sources; this is not a fresh DLL-to-upstream-archive comparison. Fonts' MiSans/Inter/SourceHan licenses already live in StreamingAssets/Licenses/Fonts. The packager also copies PHOTO_CREDITS, GROUP_CHAT_PHOTO_CREDITS, ENDING_TRIAL_PHOTO_CREDITS, HongmengOS licenses (Lucide) and LingGuangV05/ThirdParty (Blackjack). Inspect output presence rather than assuming Resources text files are conveniently readable outside serialized assets. Three selected group photos are licensed illustrations; Qinghai's CC BY-SA terms are scoped in its credit, not a blanket license for the game.

The old `StreamingAssets/LingGuang/llama/THIRD_PARTY.txt` incorrectly presents the retired Qwen3.5/Vulkan/macOS server bundle as current. This re-audit supplies an independent minimal `third-party-notice.patch` for root review; import before building if accepted. It identifies actual Qwen3/LLMUnity/LlamaLib and final license locations, retaining older notices conditionally for any still-distributed legacy payload. This text index does not replace full license texts. Current source's legacy llama directory contains only this text plus metas; no legacy executable/library was observed.

After smoke and process exit, the documented packaging command is:

```powershell
$auditTools = 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/integration-20261018/release-audit-stage'
$llmPackage = Join-Path $project 'Library/PackageCache/ai.undream.llm@9931a9ea4690'
$packageOutput = 'C:/Users/xomem/Documents/ChatGPT/games/LocalVerification/ReleasePackage/UNIQUE_RUN'
$packageScript = 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/resume-20261019/group-art/release-review/Package-VerifiedRelease-LowLoad.ps1'
& $packageScript -BuildDirectory $release -OutputDirectory $packageOutput -ProjectDirectory $project -LlmUnityDirectory $llmPackage -NativeLicenseDirectory (Join-Path $auditTools 'native-licenses')
```

It requires installed `C:/Program Files/7-Zip/7z.exe` (path exists), creates a new output outside build, uses `-mx=1 -mmt=2 -v1500m`, tests the first volume and hashes each volume below 2 GiB. It does not upload or run the game. Preserve package-result.json, SHA256SUMS.txt and READ-ME.txt. Compare copied EXE hash with the smoke EXE. This audit has not built, packaged, extracted or run any release.


## Storage and preferences before launch

Use the actual final `build-result.json`/`source-provenance.json`. Confirm `result=Succeeded`, `errors=0`, ordinary Release (no RECORDING_PREVIEW), two title/Main scenes, and exact EXE path/hash. Do not use the historical build or old `RecordingPlayerSmoke`.

`--verification-save-dir` now exists in ordinary Player (`VerificationSavePaths.cs`). It redirects chapter data, DreamOS DesktopProfile, ModelCache and Music2016. First use must be a NEW/EMPTY absolute local directory, outside the build and all normal/legacy save roots, without links. Use two separate arguments, never `--verification-save-dir=...`. Runtime logs `VERIFICATION_SAVE_ROOT <absolute path>` and creates `.lingguang-verification-save` with header `LINGGUANG-VERIFICATION-SAVE:1`, that exact path, and a GUID. Invalid paths fail closed; never retry without isolation.

**PlayerPrefs is deliberately not redirected.** Even normal startup may write `LingGuang.Music...` defaults and `LingGuang.WindowSize.Handoff2`; title settings, language, help notes and tree zoom can also write. Before launch, with every game process stopped, use `PlayerPrefs-Guard.ps1 -Action Snapshot` for the final build's exact HKCU `Software/<company>/<product>` key. After exit, Compare then Restore using the recorded exited PID, then Compare again. Preserve the CLIXML backup. Guard handles an originally absent key, exact value types, and removes only values created during this run; it refuses product subkeys or mismatched build/SID. Do not run concurrent game sessions or restore while Player is alive. This is explicit restoration, not a claim that prefs were isolated or never temporarily changed. Snapshot/restore scripts are statically parsed only, not runtime tested.

Record before/after hashes of existing user save-shaped files (`chapter-one.sav*`, slot/profile/ending JSON) under actual company/product `LocalLow/.../LingGuangV05` and the known legacy `RippleLocalPreview/LingGuang-Playability-20261008/LingGuangV05`, `DefaultCompany/competition/LingGuangV05`. Read only; never move, reset or replace them. Exclude large embedding/model/media caches from this low-load hash pass and state that scope. No need to copy models or change application identity.

```powershell
# $release is the exact successful build-result path pinned above; never a guessed latest EXE.
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

Wait for normal local service readiness before the one deliberate short free-text message. Opening the title already preloads Main and its normal chat model; embedding attachment falls back without creating a native service when its model is absent. Do not launch curl servers, llama binaries, another Editor, another Player, `SerialNativeQa`, or a second GGUF to gather evidence.

- Exact success-at-boot signal: `[灵光] 本地模型就绪：<ModelName> · Cpu · <Architecture> · GPU 层 <...> · 3 slots × 4096 · 启动 <seconds>s`. Record the whole line, actual model name, CPU DLL/backend and start time. Failure signals include `本地模型启动失败`, `本地模型不可用，改用预设回复`, `本地模型请求失败`, `本地模型服务出错`, `模型回复出错`, `模型回复解析失败`, and `Cannot start native inference`. Collect full log, not only grep successes.
- Current Windows service is CPU, `--n-gpu-layers 0 --device none --fit off`, `--threads` AND `--threads-batch` = `max(1,min(6,Environment.ProcessorCount/2))`, q8 KV cache, 2048 MiB host cache. Default `parallelJudging=false`, three routed context slots (12288 total context). `XgBrain` requests `SetParallelJudging(false)`; setting true would request four slots. **Current `Pump()` has a global `active.Count > 0 || NativeCallsInFlight > 0` guard and returns after each dispatch**, so context-slot count is not concurrent completion count. This static rule is not Player runtime peak/cancellation measurement.
- `LocalEmbedder.Attach` is still called, but current StreamingAssets/LingGuang/Embedding has **no GGUF**. `Begin()` logs `[灵光] 没有向量模型（StreamingAssets/LingGuang/Embedding/*.gguf）：检索只用 BM25。` and returns before creating its service. Confirm that message plus final build inventory; do not report embedding inference or embedding concurrency measured. If a later build adds an embedding model, re-audit: its separate service currently has one slot/2048 context and no explicit thread cap; this trace does not instrument it. Do not add/download a model for smoke.
- Use the real YY app and one available private conversation, preferably Qingwen for an unconstrained free reply; early-stage AI speech is deliberately constrained. Avoid netbar, quiz/known-trigger messages and repeated greetings, which have scripted branches. Example single request: `用一句话说说雨后窗外的样子。` Type into the observed field, send once, record sent UTC and complete returned text/UTC. Wait up to 75 seconds, no resend/retry storm. Any game-generated setup/background jobs remain ordinary behavior and are not counted as extra deliberate smoke requests.
- **Current verified-directory Player has per-request metadata tracing.** `LocalLlm` emits queued/native_dispatch/native_enter/native_return/native_task_completed/chat_callback; YY emits yy_reply_requested/yy_delivered with the same ID and actual delivery source. `native` means unmodified relative to LocalLlm's cleaned/parsed text (not raw model tokens); `native_filtered` means native completion followed by changed delivered text. fallback_null/fallback_repeat/fallback_offline, scripted_* and policy_replacement are distinct and cannot pass as native delivery.
- After complete Player exit, run the read-only helper against the exact path printed in this log, not an arbitrary JSONL. Save its JSON beside screenshots and the operator's UTC/contact/send/reply timeline:

```powershell
# Replace with the exact VERIFICATION_NATIVE_TRACE path recorded in this run.
$trace = 'C:/ABSOLUTE/THIS_RUN/PlayerData/native-verification-EXACT_ID.jsonl'
& (Join-Path $tools 'Summarize-NativeTrace.ps1') -TracePath $trace -PlayerLog $log | Set-Content -LiteralPath (Join-Path $evidence 'native-trace-summary.json') -Encoding utf8
```

For the chosen visible YY message, match UTC and contactKind to `yy_reply_requested`; require a single same-ID successful dispatch/enter/return/task-completed/native callback and yy_delivered. Helper's ordering is dispatch < enter < returned < task-completed, and returned < callback < delivered; task completion observation may occur independently of callback delivery. Review service/slot and statuses in raw events as well. Require no trace write/event-limit failure, no metadataIssues, and zero unfinished tasks/native bodies after normal exit. `structurallyValid=true` alone is not a native-answer PASS.

Record `native` or `native_filtered` exactly with `correlatedSuccessfulNativeCompletion=true`; an unchanged-answer requirement passes only unmodifiedNativeDelivery. Missing/failed/noncorrelated trace leaves origin unverified even if the bubble looks plausible. The metadata deliberately contains no prompt or response, so pair it with the actual UI observation; it does not by itself prove pixels were visible.

`observedPeakNativeBodies` / `observedPeakDispatchedTasks` are maxima observed in this exact instrumented LocalLlm run, not an all-code or all-service concurrency guarantee. Cancellation ordering stays **unverified/not exercised** unless this run actually contains cancel_requested, completion of that cancelled task and the next dispatch, reviewed in sequence. Do not manufacture cancellation or launch a stress loop for this short smoke. A successful ordinary chat does not certify cancellation, all routes, all stages or natural full-game play.

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

Use bundled library tracks; `2016 热歌` lists unbundled copyrighted songs and cannot prove playback on an empty isolated Music2016 directory. No user music is copied or downloaded. `GameMusic.OnMusicOpened` on the first real open each boot forces repeat=false, shuffle=false and starts clip **emergency_1** regardless of alphabetical list position. Record its displayed title; do not assume that is row1. It fades in and queues a written InnerVoice subtitle. Protagonist recordings are disabled in every language: VoiceLines.Play calls Stop and returns 0. Do not expect or score an opening spoken line. Cutscene-driven music ducking remains possible through GameMusic.Busy().

1. Before music app open, record 3 seconds baseline if needed (title/prologue/fan sounds are not guaranteed silence). Actually open music from desktop; wait for the initial fade and observe any written subtitle, capture current title, visible list ordering, repeat/shuffle states and two advancing progress times. One 12–15 second loopback take establishes expected audible music only over that interval.
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

Loopback includes all apps on the default output. Quiet unrelated audio. Record endpoint id/name, actual WAV+JSON SHA, timestamps and action timeline. **Listen to the WAV** for correct tune, clicks, dropouts or restart. RMS/peak/nonzero and 100ms blocks are supporting evidence only. Silence >=300ms at -60dBFS is a candidate event; compare with the initial fade, active cutscene ducking, deliberate seek/track gap, pause, shutdown fade and other apps before diagnosing a dropout. No signal metric alone gives an audio PASS.

## Finish and bounded evidence

Exit through the actual game shutdown/title flow, capture returned title, then Quit. Wait for process exit; collect exit code and complete player.log. Do not kill a model worker separately or run packaging while native cleanup is still active. If it hangs, record failure and let root decide process handling.

```powershell
# Only after the recorded Player has exited:
& (Join-Path $tools 'PlayerPrefs-Guard.ps1') -Action Compare -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml')
& (Join-Path $tools 'PlayerPrefs-Guard.ps1') -Action Restore -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml') -ExitedPlayerProcessId $player.Id
& (Join-Path $tools 'PlayerPrefs-Guard.ps1') -Action Compare -BuildResult $build -SnapshotPath (Join-Path $evidence 'prefs-before.clixml')
```

Compare normal user-save hashes with baseline. Preserve isolated PlayerData, prefs backup, build provenance, helper outputs, screenshots, logs and WAVs. Fill `evidence-template.json` per claim. Do not turn source inspection, package/build success, native readiness, or historical Editor results into actual Player inference/cancellation/audio claims. The source instrumentation gap is now addressed, but no new runtime pass is claimed by this document. Native origin, cancellation, audio continuity and scope must be filled from the final binary's actual evidence, separately. Earlier Editor fixture passes do not certify this fresh Player or a natural full playthrough.

## 2026-10-19 private/news/art integration delta

This delta changes no launch isolation or PlayerPrefs procedure. Use build version 2026.10.19.1 and the reviewed group-art/release-review packager above (SHA256 42769a446a1c9e6a8fd0732918637351bbcbf3b5a94a79c81d84774ff87aa440); retain release-audit-stage/native-licenses as its authentic notice input. That packager supplies the existing original-art/source/credits provenance checks. Do not fall back to the older packaging command if a gate fails.

Private-ending receipts live inside the same chatState, news-comment receipts inside forumState; both use normal chapter save/slot handling. No new separate save directory or registry key was added by these features. Existing save-root isolation, marker verification and PlayerPrefs snapshot/compare/restore remain required. Build source capture covers their Assets code/resources and the art Design source/manifest. CaptureSource is a pre-build file manifest, not a copy or proof that worktree edits during the build were absent: freeze production source changes from the recorded snapshot through BuildPlayer completion. Later edits cannot be attributed to this binary; rebuild when runtime content changes.

The bounded fresh-title Player smoke cannot naturally reach private endings, all three dated news shares or all thirteen group images. Keep root's exact integrated Editor/core evidence separately labelled, including their declared fixtures and untested paths; do not claim those features were exercised by this short Player run. Private/news fixed replies are authored dialogue, never native-model evidence. If these actions are reached normally in an explicitly extended run, record the actual send/receipt, save/reload and no-duplicate behavior without injecting progression or editing ordinary user saves. Do not expand or repeat prior UI suites solely to satisfy this document.

No new third-party text/image dependency was found in the private/news code: the news shares send existing catalogue title/source links and new fixed authored replies; private forwarding preserves the player's approved in-game answer. Ten new group illustrations are project-original and their existing credits/source manifest must ship through the selected packager. This is a source audit, not a new legal opinion, binary-license match or final payload inspection.
