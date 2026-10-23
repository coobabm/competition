# Archive runner bootstrap ordering and diagnostics

Runner-only candidate based on production after the earlier `EnsureInitialized` patch. No production runtime change, no forced `EnsureReady`, no retry, no timeout extension. Apply `ending-archive-bootstrap-order.patch` to the production project after review.

## Failure mechanism established in source

The 20261018-223310-728 report still failed at `pre-choice Main loaded`, with no runtime errors. Its title archive assertions passed. The runner loaded Main, waited for the async operation and three extra frames, and only then called `MainSceneEntry.EnsureInitialized`.

`XingGuangController.Initialize` calls `TakeOverWindow`, which registers its custom view through `ChapterOneDesktopBridge.UseCustomView`. It does not create `View`. `View` is created in `EnsureReady`, normally reached by the native bridge's one-time `Start -> EnsureApp`. If custom registration occurs after that Start, the earlier native view has already been warmed and replacing the binding does not schedule another Start. Waiting longer does not build the replacement view. A subsequent real app open would build it, but the runner waits before doing that.

Normal title `CaseMenuMainDisplay.Configure` runs synchronously during `sceneLoaded`, calls the production bootstrap and `PrepareDesktopShell`, and pauses gameplay before Start. The runner-only fixture omitted this ordering. Source therefore supports a harness lifecycle defect; this candidate does not establish a normal title-flow product bug.

## Change

For the single explicit pre-choice fixture reload only, the existing `sceneLoaded` hook installs disabled model components, invokes the production bootstrap synchronously, prepares the existing desktop shell, and quiets the already-declared historical story drivers before Start/Update. Native bridges still build the actual custom views through their ordinary Start. The callback result/error is asserted after the load; nothing retries or builds a substitute view.

The source pre-choice prologue/install state, isolated save path, stage, memory-review receipt, single scene-local controller (including inactive search), and native binding ownership are asserted before the existing readiness wait. Existing runtime/fixture boundaries remain unchanged.

## Diagnostic evidence

Each report links `lifecycle-XX.txt` snapshots before bootstrap, immediately after bootstrap but before Start, after normal Start frames, and on any subsequent timeout. These include all runtime/controller objects including inactive ones, active scene/frame, private initialization flags, shell-prepared state, disk/tick/TestMode, save path/status, prologue/install/branch identity and lab hash, router custom bindings/content/windows, and registered native windows. They distinguish attachment failure, hidden objects, lazy view state, source misload, and later presentation failure without inferring a pass from `EnsureInitialized` returning true.

Original failed reports and immutable source evidence are untouched. No Unity, compile, test, Player, model or audio was run. Static packaging reports balanced delimiters and no baseline drift; clean application is checked separately. Root must run the actual UI suite and inspect the new lifecycle snapshots before claiming resolution.
