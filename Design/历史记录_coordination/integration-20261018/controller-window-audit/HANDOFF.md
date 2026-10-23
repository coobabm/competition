# Controller and window-index audit

## Ready first: controller-markers.patch
Six static markers wrap existing Update sections without changing calls/order:
- Xg.Controller.CalendarProfile
- Xg.Controller.BrainTick
- Xg.Controller.SimTick
- Xg.Controller.OpeningVoice
- Xg.Controller.ProgressEvents
- Xg.Controller.ChapterCaptureSave

The new capture's 155.55 ms estimated self belongs to the whole managed Update, not to a proven child function. These markers are diagnostic; they do not claim a fix. They preserve all original guards and save gating.

## Confirmed remaining repeated full-tree work
DesktopWindowIndex.Get enumerates every loaded scene's roots and calls GetComponentsInChildren(true) on every root every 0.5 seconds. GetBindings repeats a full traversal every second. Sharing buffers avoided result-array allocations, but did not avoid hierarchy traversal. DesktopWindowDefaults and StudyDesktopGates pay whichever scan is due. This matches their 10–17 ms periodic marker peaks, but a marker capture is still needed to measure exact subtree attribution.

## Separate candidate: window-index-registry.patch
Three files only: DesktopWindowIndex, WindowManager, ChapterOneDesktopBridge.
- Initial discovery includes inactive loaded-scene descendants and the runtime owner's persistent root.
- SceneLoaded discovers new additive/replacement scene objects. Subsystem reset clears references/counters and safely replaces its one scene callback; first query reseeds even with domain reload disabled.
- WindowManager Awake/OnEnable register; OnDestroy unregister. No OnDisable removal, preserving hidden/closed windows.
- Bridge Awake/OnEnable register and OnDestroy unregister. All three Configure paths register both the bridge and assigned window while inactive. This includes the current Zhongbao factory, which configures while inactive and then activates.
- Every query prunes destroyed fake-null objects (including never-enabled objects whose OnDestroy does not fire). No per-query full-scene scan. Snapshots rebuild only when membership changes, so registration while callers enumerate does not invalidate an existing list.
- Registration stores components only, no serialized fields or save migration changes. Existing gate checks and polling cadence remain unchanged; old-save rebind sees current components, and dynamically activated windows appear through OnEnable.
- Profiler marker DesktopWindowIndex.DiscoverTree covers the discovery operation.

Explicit boundary: arbitrary newly instantiated never-enabled inactive windows that bypass Bridge.Configure cannot receive Awake/OnEnable yet. Such factories MUST call DesktopWindowIndex.RegisterTree(newRoot) (or Register(window)). Current DesktopBridge runtime source search found no native WindowManager clone factory; Zhongbao's inactive bridge construction is covered through Configure. This requirement is not equivalent to old universal periodic discovery, so review future/third-party clone factories before merging. Do not describe it as automatic coverage of every possible hidden clone.

WindowScans/BindingScans now count root traversal calls, not periodic passes; tests use stable/no-increase semantics only.

## Checks
window-index-check-migration.patch removes the obsolete reflection into nextWindows and explicitly exercises the inactive-factory registration boundary. This is a changed discovery contract, not a claim that the old hidden-clone polling test passes unchanged. Existing sizing and player move/resize assertions remain.

Before declaring safe: run DesktopHotpathChecks and actual PlayMode hidden/active window open-close, dynamic Configure, additive scene, never-enabled destroy, domain reload reset and old-save rebind tests; ensure later windows receive default size/gating and a player-resized window stays unchanged. A dedicated lifecycle fixture is still needed if broader proof is required.

All three patches passed git apply --check against current main. No compilation, Unity run or production edit by this agent. Candidate registry patch should be reviewed/validated separately from already delivered diagnostic markers.
