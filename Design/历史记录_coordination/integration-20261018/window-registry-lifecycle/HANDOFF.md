# Window registry dedicated lifecycle runner

Patch: window-registry-lifecycle-checks.patch. Two Editor-only files, based on existing registry batch runner. NOT compiled/executed. Apply-check passed.

Execute WindowRegistryLifecycleRunner.Run in dedicated -batchmode Editor WITHOUT -quit. It creates an unsaved empty scene and a uniquely named temporary additive fixture scene at Assets/LocalBuild/Editor/WindowRegistryFixture-<guid>.unity. The temporary scene is deleted on normal runner exit; if the Editor is externally killed, its uniquely named artifact may remain. No real scene/save is loaded, and the verification save override is isolated. No change to actual Enter Play Mode settings.

Tests include:
- initial hidden discovery, stable snapshot reuse and 1000 queries without rescan;
- new inactive bridge Configure registers itself and assigned window;
- actual WindowManager Awake/OnEnable on a newly activated window, real bridge activation with no-op view; disable retains both;
- never-enabled explicit factory registration, never-enabled destroy pruning, activated destroy removal;
- REAL saved additive scene load via LoadSceneAsyncInPlayMode, inactive discovery through actual sceneLoaded, actual unload/pruning;
- direct invocation of real SubsystemRegistration Reset and discovery of surviving hidden windows/bindings;
- initial default browser sizing, manual player position/size changes, in-memory default/legacy simulation replacement plus registry Reset preserve sizing;
- replacement runtime owner discovers its hidden subtree without losing old windows.

Limits: the legacy check substitutes a default ChapterOneSim in memory, NOT an on-disk old-version save migration claim. Window callback fixture deliberately sets mobile mode to avoid unrelated preferences, taskbar and animation, while using the actual window callbacks; production visual sizing/open animation still needs game acceptance. Actual reload-option permutations are not simulated. Outer runner times out after 60s and errors fail the run.

# Expanded factory audit

Searched all Assets C# sources (not only DesktopBridge) for AddComponent<WindowManager>, typed Instantiate<WindowManager>, typeof(WindowManager) addition, Instantiate window-name expressions, and all untyped Instantiate call sites. Direct component creation found only LocalBuild DesktopHotpathChecks fixtures. Runtime ChapterOneDesktopBridge construction is the inactive Zhongbao factory; Configure paths now register both components.

DreamOS uses many generic Instantiate sources (mail content/viewers, browser page content, Game Hub gamePrefab, widgets and library rows). Feel also has generic arbitrary GameObject instantiators. Type-only search cannot exclude them. To investigate current configured assets, scanned all Assets prefab files containing WindowManager's script GUID 0c53c90e36e396b46ac4cb01201a9a5f: 16 prefabs; resolved their GUID references in all .unity/.prefab/.asset files (18,702 reference lines, mostly nested prefab structural overrides). No nonstructural serialized reference to those 16 full-window prefab GUIDs was found. Detail: window-prefab-reference-audit.json. This is evidence for current checked-in asset wiring, NOT proof that a future generic factory, mod, dynamically loaded asset or cloned scene GameObject cannot create a never-enabled window.

Registration contract remains explicit: arbitrary newly created never-enabled inactive whole-window clones bypassing Bridge.Configure must call DesktopWindowIndex.RegisterTree(newRoot). The candidate patch is safe for identified production factories but broad generic factory coverage must not be claimed. Do not instrument all unrelated generic instantiators without a demonstrated window use.
