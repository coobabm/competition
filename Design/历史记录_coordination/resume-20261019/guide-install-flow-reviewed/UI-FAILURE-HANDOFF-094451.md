# Files / Guide UI failure handoff — frozen for Claude

Status: investigation frozen at main-agent request. No production edits, new candidate patch, Unity invocation, or new test execution. Original failed report remains authoritative:

`C:/Users/xomem/Documents/ChatGPT/games/competition-latest-20261016/LocalVerification/SidebarLayout/20261019-094451-825/report.json`

It records 32 check entries, 24 screenshots, no runtime/editor infrastructure errors, and failure at `FilesWindowUiChecks.cs:227`: `guide import note became visible within deadline`. Entries include capture records, so 32 must not be advertised as 32 independent functional cases. Earlier batch drop checks completed; the new Guide segment did not pass.

## Demonstrated fixture defect

Production read-only source:

- `Assets/LocalBuild/Editor/FilesWindowUiChecks.cs`, guide regression block around lines 207–227: creates `new XgSim()`, sets only epochs=1, assessments=1 and treeIntroDelivered=true, then purchases train.curve, train.data and spam.pack and advances download. It does not establish any existing labelled training samples or a teaching-flow fixture that opens Train.
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Features.cs`: without teachingFlow, FeatureVisible("train") requires TrainingUnlocked(Vision) or TrainingUnlocked(Sequence). Setting epoch/assessment counters or buying train.curve does not itself satisfy that branch.
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:609`: TrainingUnlocked requires an available dataset with Samples >= XgCatalog.SamplesToTrain.
- `Assets/LingGuangV05/XingGuang/Core/XgGuide.cs:183`: PendingDataImport returns null when TabOpen(sim, Lab, "train") is false. This preserves the product page gate.
- The corresponding passing core fixture in `Assets/LingGuangV05/XingGuang/Tests/Editor/XgGuideTests.cs:23` has 300 pre-existing spam labels in Trained(). The UI fixture omitted that prerequisite. Its claimed already-trained/assessed state is internally inconsistent with having no training samples.

This missing Train prerequisite is a deterministic fixture defect sufficient to prevent the expected data.install task. The failed run did not record the actual TopStep/TopSim at timeout, so no claim is made that this was the only runtime obstruction.

## Binding and visibility review

`FilesWindowUiChecks.Bind` replaces ctl.Sim, binds/refreshed ctl.View and binds Files to that Sim. GuideNote caches the controller, not the old Sim; its Update reads lab.Sim. No stale-Sim binding defect was established from this source review.

`Assets/LingGuangV05/DesktopBridge/Story/GuideNote.cs` updates TopStep and TopSim before its busy branch. If ready is false, it returns without updating those static properties. Readiness requires nonexclusive story focus, a real non-prologue runtime, no cutscene, controller/Sim, and PrologueDirector.Desk. It may also show the setup task if SetupShowing. Its completion animation can make busy true while steps are pending. The current timeout checks those fields but reports none of their values.

`SidebarLayoutChecks.EnableFixtureEconomy` stops several unrelated narrative components (StoryDirector, StoryDesktopPresenter, PrologueDirector, SpiderIntroScene, LoveQuestionCutscene, StageOneTutor, LaoZhouYY). It does not stop every narrative source. The inspected `files-11-real-batch-installed.png` shows a 喵鱼 notification, while the note remains visibly present and shows the previous opening-training task. That notification alone is not proof of exclusive story focus or the timeout cause. No justification yet exists to globally disable more product behavior or override StoryFocus probes.

## Suggested next bounded work (not implemented)

1. Make the new Guide fixture satisfy the existing Train-page gate consistently. Prefer a declared minimal preparation through existing label/teaching APIs; distinguish setup actions from physical UI input. Do not mark the downloaded pack installed or inject its imported sample count. If using explicit pre-existing labels, document that fixture honestly and establish samplesBefore only after setup.
2. Before waiting, assert the controller/View/Files binding, Train visibility, ready uninstalled file, and XgGuide.Current with the note's actual household inputs. Add bounded timeout diagnostics containing TopSim identity, TopStep id, busy, note activity, controller identity, SetupShowing, StoryFocus.Exclusive and relevant cutscene state, plus a timeout screenshot. Read diagnostics; do not assign TopStep/TopSim or invoke private Show/Apply to force a pass.
3. Keep actual GuideNote row LCD raycast/click, navigation-only no-install/no-fee assertions, and actual source-to-Train LCD drag/drop. Keep existing import gate and sample-increase assertions. Do not replace these with Follow(), GoLab() or InstallDataFile() calls.
4. Compile/review an isolated small candidate before main-agent integration. The main agent owns the Unity rerun. Until its report passes, Files/Guide remains failed/unverified; public-reading tail, ending-A transaction, casino UI and Player remain unverified as separately tracked.

No patch is delivered for this failure. The earlier six-file product candidate is already integrated and must not be blindly reapplied; this handoff concerns the current UI regression fixture and diagnostics only.
