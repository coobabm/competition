# Optional Main logic-tools UI stage

Prepared candidate only. No Unity, C# compilation, tests, model execution, or runtime screenshots were run by this agent. Root must import and run serially in its isolated Editor workflow.

Root's first import compilation found a missing `using LingGuangV05.Runtime;` for GameText. Root added it in production; the candidate now mirrors that one-line fix. The original static delimiter check did not detect this name-resolution error. Do not reapply the full patch over root's imported files.

## Package and entry points

Apply `board-logic-tools-ui.patch` from the production project root. It changes the existing `Assets/LocalBuild/Editor/BoardGamesUiPlayQa.cs` and adds `BoardGamesUiPlayQa.LogicTools.cs` plus its meta. The existing runner becomes partial; root's `Application.targetFrameRate=30;QualitySettings.vSyncCount=0;` line is preserved exactly. Shared Sidebar files and production Assets were not edited.

- `BoardGamesUiPlayQa.RunLogicTools`: optional tools stage alone; 360-second stage deadline after Main boot.
- `BoardGamesUiPlayQa.RunWithLogicTools`: original full native games followed by tools; 1080-second stage deadline.
- `BoardGamesUiPlayQa.Run`: original games-only mode, unchanged coverage and 720-second stage deadline.

Requires imported picture-source, chess-composer, and Xiangqi-composer packages, including both composer QA helpers and their 12/16-second generation waits for the 0.1-second candidate cadence. Xiangqi coverage is the restricted general/general/rook mate-in-one composer, not full Xiangqi.

## Input and save scope

The new stage uses synthetic Unity EventSystem input on the actual Main UI and actual LCD render target. It is not physical OS mouse input or Player-build certification. Public-image drag starts from the current top raycast hit, dispatches pointer-down/initialize/begin-drag, moves over eight rendered frames, and routes drop from the current top hit up its actual hierarchy before end-drag. It never directly calls the picture reader, `TryTake`, `TryDrop`, `Accept`, or target callback. Closed targets must be absent from routing. Clipped source images are exposed using routed ScrollRect scroll events; masks and raycast behavior stay enabled.

Bodu navigation opens the real application, sets the native search input to `gomoku`, clicks its actual Go control and public club link. Tieba navigation clicks the actual public puzzle topic. Search text assignment and initial application opening are fixture setup. Native windows are arranged side by side programmatically, not by a physical resize interaction.

Four isolated household snapshots are serialized under the existing output `Save/` directory: unpurchased, Lv2 A, Lv2 B, and Lv4. Each switch reads the saved JSON into a new ChapterOneSim and runs the existing runtime/controller rebind, asserting a replacement XgSim reference. This exercises actual serialized state replacement; it does not test title-screen/save-slot UI. Stage, money, and unlocks are fixture setup, not tech purchase interaction tests. Story/model activity and simulation ticking remain disabled by the existing isolated runner.

## Intended assertions and artifacts

For each Bodu/Tieba source in Chinese and English, the stage checks exposed source corners, distinct native windows, eight recognized image stones and F6, no samples/rounds/files, closed target rejection, closed source cancellation, old-save drag rejection after rebinding to saved B, a successful fresh B drag, and hidden entry/target in an unpurchased save. It captures `*-before-drag.png`, `*-recognized.png`, `*-stale-save-rejected.png`, and `*-unbought.png` for each source/language.

Both existing ComposerUiQaHelper iterators receive the actual saved-unpurchased rebind callback. Their direct stale-button callback probes remain explicitly guard-only checks; normal composer entry, piece selection, reset, and reopen use canvas hit-checked controls. The wrapper captures generated and solved boards plus the final new-save unpurchased page in both languages and inspects title/help/status/button TMP geometry. Expected completed tools run: 28 screenshots, fixture JSON files, and the existing result.json/passed.txt reporting. A failed run may produce only a subset; runtime completion is not claimed.

## Static validation

`package.py` is rerunnable and regenerates the patch/manifest, checks C# delimiter balance, and reports baseline drift. The one-shot `build.py` must not be rerun over this candidate. Static packaging and `git apply --check` do not establish compilation or UI success.
