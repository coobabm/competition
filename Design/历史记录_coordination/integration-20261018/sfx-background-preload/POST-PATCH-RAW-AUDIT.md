# Read-only post-patch raw profiling audit

New capture: `20261018-121139-893-961172`, raw export `raw-analysis-20261018-121446-441` (complete.txt present).
Prior clean capture: `20261018-113929-404-63a1f6`, raw export `raw-analysis-20261018-114326-603`.
All numbers below select the real `/Main Thread`, not worker waits. No Unity execution, compilation or production changes performed for this audit.

## Identity registry change

|Phase|Earlier max LateUpdate ms|New max ms|New mean per valid thread frame ms|New >=2ms peaks|
|---|---:|---:|---:|---:|
|teach-continuous|13.335100|0.197900|0.009468|0|
|train-auto|12.660200|0.374000|0.009888|0|
|yy-tieba-switch|12.692400|0.344400|0.010451|0|

The old once-per-second large AiDesktopIdentity peaks are absent in this capture. This is observed behavior of these runs, not a guaranteed universal bound.

## Synchronous audio load paths

|Phase/marker|Earlier calls|max ms earlier|New calls|max ms new|
|---|---:|---:|---:|---:|
|teach SoundManager.LoadFMODSound|10|11.859200|10|0.015400|
|teach File.Read|369|1.320900|10|0.037300|
|train SoundManager.LoadFMODSound|15|21.986099|0 (marker absent)|n/a|
|train File.Read|451|16.978300|6|0.477600|
|train AsyncReadManager.SyncRequest|451|17.080601|6|0.552900|

All three new phases contain zero >=2ms main-thread LoadFMOD/File.Read/SyncRequest samples in the peaks export. Remaining reads must not automatically be attributed to audio. New run explicitly enabled -forceAudio; multiple fixes and warm cache conditions also changed between runs, so this is not an isolated causal benchmark of the audio patch. Runtime wait/isPlaying checks likewise cannot establish audible continuity.

## Remaining training stall

Frame 123 is the actual 227.592102 ms main-thread frame, containing 387,691 samples.

- `XingGuangController.Update`: inclusive **211.180603 ms**, estimated self **155.546604 ms**. Parent path: Main Thread > Application.Tick > Application.UpdateScene > UpdateSceneIfNeeded > UpdateScene > PlayerLoop > UpdateScene > Update.ScriptRunBehaviourUpdate > BehaviourUpdate.
- Two >=2ms child Mono.JIT samples: 2.743200 and 2.555400 ms. These do not explain the full self time, and the peaks export excludes numerous sub-threshold samples. Inclusive minus estimated self is 55.633999 ms; that is not all JIT by available evidence.
- Separately, UGUI.Rendering.UpdateBatches is 8.798300 ms inclusive / 5.155200 ms self in this frame.
- There is no >=2ms synchronous audio loading path left in this frame/export. The large training stall persists despite the prior audio/identity peaks disappearing.
- Current ordinary (non-Deep) profiling does not name managed subcalls within the 155.55ms self component. Source Update includes Sim.Tick/brain handling, progress events, profile updates and chapter capture, so it would be incorrect to declare any one of those the cause from this export alone. A focused marker capture around those boundaries is the next diagnostic if further work is authorized; no change made now.

Other new training frame: 1004 is 63.521000ms total; controller inclusive42.995899/self21.853200, child GC.Collect18.720900ms. This is a separate non-first-frame stall.

Remaining unrelated periodic script peaks include DesktopWindowDefaults.LateUpdate (up to17.271799ms in YY/Tieba,12.305900ms train) and StudyDesktopGates.LateUpdate (11.772900ms YY/Tieba,10.440300ms train). These are reported, not modified. Teach also has FontEngine.Get_GPOS_Lookups up to12.982500ms and GC21.149500ms. Overall averages must not be used to claim the game is now smooth.
