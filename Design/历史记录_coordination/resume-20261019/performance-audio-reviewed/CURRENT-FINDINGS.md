# Current Editor profiler findings — 2026-10-19

Status: saved at the user's stop request. Read-only review completed; no production edits, new optimization, new tests, Unity runs, or builds were performed for this report. Further verification is paused.

## Evidence and limits

Summary: `current-20261019-092802-summary.json` in this directory. Capture: `competition-latest-20261016/LocalVerification/IntegrationProfiler/20261019-092802-102-66ee98`; RAW export: its `raw-analysis-20261019-093254-815` directory. Both completion markers exist. Recorded source was commit `3533141a406940d571e064f3797328c7d214235a` plus dirty source, Unity 6000.4.5f1.

This is Editor PlayMode with an isolated save and local LLM disabled. Teaching uses visible control callbacks rather than physical mouse input. Training uses scheduled BeginEpoch requests rather than naturally purchased automation. Each phase has a 3-second warm-up and a 30-second measurement. The capture predates the two Instance substitutions and Guide changes, so it cannot establish their measured benefit. Rendered-frame deltas and RAW main-thread frame times are different measurements.

| Phase | Rendered / RAW main frames | Completed / attempted | Rendered mean / p95 / max (ms, rounded) |
|---|---:|---:|---:|
| Teaching | 1665 / 1669 | 81 / 81 | 18.006 / 25.311 / 65.417 |
| Training | 1728 / 1731 | 16 / 29 | 17.342 / 22.445 / 65.117 |
| YY / Tieba switching | 1550 / 1554 | 20 / 20 | 19.326 / 32.840 / 105.621 |

## Lifecycle review of the two substitutions

Reviewed `ZhouCardLoan.cs:51` and `AjiePhoneSort.cs:43`, replacing FindAny lookup with `XingGuangController.Instance`. No blocking lifecycle regression was found for the current single-Main, single-runtime product model.

- Controller Awake and Initialize assign Instance. It resides on ChapterOneRuntime, not a window root, so closing or minimizing windows does not destroy the controller.
- BindSave replaces the controller's Sim; both consumers resolve the current controller and current Sim each time rather than permanently retaining the old Sim.
- Existing Unity null checks and Update guards remain. OnDestroy clears Instance only when it still references that controller, preventing destruction of an old controller from clearing a newer reference; it also unsubscribes runtime events.
- Consumers retain their half-second CheckEvery cadence, other callback accesses, and OnDisable cleanup.
- The title's additive Main preload reuses Main. Pausing disables runtime MonoBehaviours; FindAny also locates disabled components on active GameObjects, so that pause does not itself introduce a lookup difference.

Scope limitation: this is not proof of equivalence for arbitrary inactive roots or multiple controllers. FindAny excludes inactive objects by default; Instance can retain an inactive component. DisallowMultipleComponent prevents duplicates on one GameObject, not across all GameObjects. No new production path introducing those configurations was identified. No additional change is requested on that hypothetical basis.

## Product and tool contributions to long frames

Times below are RAW main-thread milliseconds. Inclusive parent and child samples must not be added together.

- Teaching frame 115, 65.331 total: TextMeshProUGUI.OnDestroy 32.165 inclusive / 9.922 self, containing GC.Collect 22.219; UGUI batching 9.643 inclusive. This is a product UI lifecycle / engine GC path, not evidence that the whole frame is test-tool overhead or that one specific product method caused all GC.
- Training frame 1289, 64.621 total: ZhouCardLoan.Update 20.133 inclusive / **19.627 self**, AjiePhoneSort.Update 18.705 inclusive / **18.036 self**. The previously cited 19.63 / 18.04 numbers are self times. Profiler.FlushMemoryCounters contributes 2.232 tool overhead. These peaks motivate the reviewed lookup substitutions but do not measure their post-change savings.
- Training frame 124, 55.079 total: XingGuangController.Update 36.490 inclusive / 0.016 self contains simulation / epoch work; a nested Mono.JIT sample is 2.200 inclusive. The exported peak list is not a full child-call breakdown, so this JIT sample cannot explain the entire controller duration.
- YY / Tieba frame 1041, 100.736 total: TiebaView.Update 47.745 inclusive / 5.900 self, plus UGUI batching 11.918 inclusive and ScrollRect layout / prerender work. Frame 1186 similarly has TiebaView.Update 39.696 inclusive / 5.314 self. These are product UI refresh peaks; the available child breakdown does not isolate the precise redraw method.
- YY / Tieba frames 1406 and 1108 have IntegrationProfilerCapture.Tick 69.268 and 64.977 inclusive, respectively. Tick includes both active switching calls into product code and capture-tool work: it cannot be entirely deducted as overhead or attributed to ordinary product Update.
- Teaching frame 78 includes Tick > AddComponent > TMP.OnValidate > GC, explicitly involving Editor validation. Other training peaks include Profiler.FlushMemoryCounters, UGUI geometry / batching, clipping, and rendering work. Attribution is mixed rather than uniformly product or uniformly tooling.

## Audio and synchronous reads

All three phases have invalid negative Audio.Thread recorder aggregates, already flagged in the RAW anomaly evidence. They are unusable as audio timing measurements.

Exported main-thread peaks: teaching SyncRequest 1.542 ms, File.Read 1.420 ms, SoundManager.LoadFMODSound 0.020 ms within the capture-triggered loading path; training SyncRequest 0.577 ms and File.Read 0.454 ms in the view loading path. The YY / Tieba export has no entries for these selected markers. No selected exported main-thread read peak reaches 2 ms. This does not establish uninterrupted audible playback.

## Remaining work, paused

The existing final acceptance must cover the integrated changes and actual Player behavior. No stable-60-fps, post-fix performance, inference-source, or audible-continuity PASS is claimed here. Player/audio verification and any final integrated rerun remain with the main agent; this report adds no new optimization or test requirement.
