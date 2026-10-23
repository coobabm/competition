# First evidence-backed candidate: periodic AI-name download discovery

Source evidence: IntegrationProfiler/20261018-113929-404-63a1f6/raw-analysis-20261018-114326-603.
- teach sample-peaks: AiDesktopIdentity.LateUpdate >=2ms 28 times; frames49,103,158,213,267,321,379,436... about one second apart. Frame779 inclusive13.3351ms, self12.8896ms.
- train: 29 such peaks; frame122 self12.1806ms, followed177,235,294,351,408 etc.
- production AiDesktopIdentity Refresh executes a global inactive-inclusive FindObjectsByType<WebBrowserDownloadItem> once a second even when neither name nor downloads changed. Other per-.2sec work remains; therefore repeat measurement must establish actual effect, not assume the full spike disappears.

Patch removes only repeated global download discovery. WebBrowserDownloadItem.KnownItems seeds once (covers inactive scene rows and domain-reload-disabled sessions); Awake/OnEnable registers newly activated rows; OnDestroy removes them; inactive completed rows retained. Identity still updates the same download display labels each second, fileName persistence key unchanged, null-safe. A newly instantiated row under an inactive hierarchy registers on first activation; its label is updated on the existing one-second identity cadence. Other browser behavior unchanged.

Not compiled/run. git apply --check passed. Suggested real regression: create/download AI installer, rename AI, hide/reopen downloads, completed row rename, destroy/recreate browser rows, reload isolated save/language; display follows name while fileName persists. Verify scan no longer repeats and same-period identity peaks reduce in clean raw, with sound enabled. Initial seed still has one global scan.

Other actual evidence (not included in this patch):
- train frame121 XingGuangController.Update inclusive207.483ms/self131.930ms. Child File.Read16.849ms path SoundManager.GetHandle > SoundManager.LoadFMODSound > AsyncReadManager.SyncRequest > File.Read.
- train frames154,270,389: XgAssessmentClock.Update also invokes synchronous sound loading, File.Read15.24/16.89/16.86ms. This is separately attributable production/Editor asset loading, not a blanket UI-render claim. Confirm player import/preload behavior before broad music changes.
- teach frame61 FontEngine.Get_GPOS_Lookups17.224/16.612ms under TryAddCharacter; frame753 GC.Collect14.012ms. Missing-glyph fix already pending/done on main; do not duplicate it or call the new capture post-fix.
- Blank sample-name worker peaks are unlabelled waits/unattributed samples and cannot identify a business method. Filter actual Main Thread and inspect paths. Editor Application.Tick/Profiler overhead is not player FPS.
No claim that music stutter is fixed; audible continuity still requires real playback evidence.
