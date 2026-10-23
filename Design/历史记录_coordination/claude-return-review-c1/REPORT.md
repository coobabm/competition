# C1 review — ACCEPT FOR INTEGRATION AND TESTING

Read `ClaudeReturn/README.md`, C1 REPORT/QUESTIONS and the 1750 HANDOFF. No setting/design conflict identified. No production file was changed, and no Unity, model, compiler or test runner was launched by this review.

## Baseline / overwrite safety

Both current production files are **byte-for-byte equal** to their 1750 source snapshots, and snapshot hashes match `source-manifest.json`:

- `Assets/LocalBuild/Editor/FilesWindowUiChecks.cs`: `78c8f50098ce5dbb96ece42110900969dfcdbc1b22e2b1286cc066af16e6a54c`
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgGuideTests.cs`: `86effe229b22c4dc95ade82744a78a28a0597b195218dff623af07d1ce568c66`

Therefore there is no post-handoff work in these two files to overwrite. Candidate content matches Claude's return exactly; generated patch contains only its hunks. Shared `SidebarLayoutChecks.cs` is not copied into the candidate. A separate optional one-line scope patch describes hand-label setup and explicitly identifies this guide segment as Chinese-only.

## Technical review

- Root-cause chain matches current product source: `PendingDataImport` in XgGuide requires `TabOpen(train)` after paid import unlock; `TrainingUnlocked` requires `XgCatalog.SamplesToTrain` samples on an available dataset. The old UI fixture set epochs/assessments but had no labels. This proves the missing prerequisite, not that every other host gate is clear.
- New setup uses real `SelectDesk/Card/Answer` through the isolated real host, and checks labels/Train availability plus no data file or installed batch. It does not inject imported samples, call InstallDataFile or assign TopStep/TopSim. Setup is explicitly labelled non-LCD prerequisite work. Normal correct-answer income occurs before the existing fixture wallet reset to1000 and before purchase/navigation baselines; no extra UI-earned-income claim is made.
- Existing paid BuyNode and ordinary download Tick preparation remain; each purchase checks its exact wallet debit. Core regression compares otherwise equivalent unlabelled/labelled fixtures and checks that import advice never targets a closed Train page. It tests a real prerequisite difference rather than claiming natural story progression.
- Binding checks, pure guide/household reading, rendered row/save/file identity, visible windows/folder and interactable button assertions are appropriate. All newly referenced fields/methods/properties exist in the current source: GuideNote House/busy/rows/shownFor/note/titleBar/lab/runtime/collapsed/CutscenePlaying, Row rt/step/label/button, XgGuide Wallet money/vram and PendingDataImport, view Controller, Files breadcrumb/ready/payload, and drop LastInstalled. This is source verification, not C# compilation.
- Real navigation still calls existing `Click`, which maps through the LCD bridge, validates the actual click-handler raycast and sends pointer down/up. It then proves no installed flags/batches/samples/labels, wallet, unlock count or active drag changed. The original 6s/4s deadlines are unchanged; added diagnostics only read gates and capture failure views.
- Real import still calls `BeginDrag`/`EndDrag` on the LCD surface. It checks the payload's exact owner and single file ID, no install on drag start, actual training drop-target raycast, LastInstalled==1, payload cleared, samples/batches increased and no second fee. Re-drag of the installed row must produce no payload or further samples/fee. Original earlier cross-save, revoked-gate, paid and duplicate-install checks remain unchanged.
- Existing `TheDataStepNamesTheSmsPackThenTheLabelsThatCountInFull` already contains the installed-batch baseline correction in both 1750/current source; Claude retains it. Do not reapply the older failing version from historical candidates.

## Artifacts / validation

- `c1-reviewed.patch` SHA256 **87328a30a9aafe11a662af7aed0ee3f15cc33158fbcecae2abc42e2fb2e6f5c0** — two QA/test files only.
- `optional-sidebar-scope.patch` SHA256 **df1df56c1547052a385c58d07377d77363725882d136b97d69f94c120effa1ec** — one descriptive shared-host string only; separate so other Sidebar work stays intact.
- `baseline/`, `candidate/`, `manifest.json`, `prepare.py` preserve review inputs and exact hashes.
- `git apply --check` passed read-only for both patches against current production. No compile/test/UI pass claimed.

## Exact pending test entries

Parent should integrate only when its serial Editor run has exited, compile the affected test/Editor assemblies, then run:

1. NUnit/EditMode class **`LingGuangV05.XingGuang.Tests.XgGuideTests`**, including new `TheUiImportFixtureNeedsTheTrainPageThatItsImportStepPointsAt` and existing `ReadyDataUsesPaidImportPrerequisitesThenOnlyTheExplicitDragStepInstalls`.
2. Actual UI entry **`FilesWindowUiChecks.Run`**, which delegates to `SidebarLayoutChecks.RunFiles`. Use the actual foreground serial runner and freeze C# edits during execution.

Existing launcher commands, for parent execution only (not run here):

```powershell
& 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/resume-20261019/run-unity-check.ps1' -Method 'FilesWindowUiChecks.Run' -LogName 'claude-c1-guide-core-UNIQUE' -TestFilter 'LingGuangV05.XingGuang.Tests.XgGuideTests'
# Wait for that Editor to exit before starting the foreground UI run.
& 'C:/Users/xomem/Documents/ChatGPT/games/.coordination/resume-20261019/run-unity-check.ps1' -Method 'FilesWindowUiChecks.Run' -LogName 'claude-c1-files-ui-UNIQUE' -Foreground
```

Require the complete Files report to pass, then visually review `files-12-guide-ready-before-click.png`, `files-13-guide-navigation-before-drag.png`, `files-14-guide-real-drop-installed.png`. Failure captures are `files-12a-guide-note-timeout.png` and `files-12b-guide-navigation-timeout.png`; use the gate diagnostics instead of extending deadlines. Current new guide segment is Chinese only; earlier suite bilingual coverage does not make this new segment bilingual. Ctrl-key automation, physical OS drag and natural progression remain outside its claim. No ordinary Player/release acceptance follows from this editor fixture.

Scope remains C1 only; C2/C3 and production product behavior are untouched. GuideNote's existing PlayerPrefs seen-place behavior remains an external runner/prefs-protection concern, not a newly introduced C1 mutation or permission request.
