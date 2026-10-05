# 灵光.exe 接入与第一章闭环 — 2026-10-03

Scope approved by user: integrate the existing 2010desktop and implement chapter one, not all six chapters. No model download, paid/network inference, vendor edits, or old LingGuang edits.

## Isolation and ownership
The requested live Unity project has uncommitted and untracked desktop assets. A clean Git worktree would omit those assets. Work additively in `/Users/kk/competition/Assets/LingGuangV05/` and a copied `Scenes/Main.unity`; preserve original 2010desktop and project settings. Never save over original scene. Parent agent alone controls Unity Editor and scene changes. No commits/pushes in this task.

- Core worker: `Core/`, `Tests/Editor/CoreTests.cs`, Core test asmdef, `Tools/LingGuangChapterOne.Tests/`.
- UI worker: `UI/` and its asmdef only.
- Desktop worker: `DesktopBridge/` and `Editor/ChapterOneInstaller.cs` only, no asmdefs there so adapter can reference DreamOS Assembly-CSharp. Write builder but DO NOT execute it.
- Parent: `Runtime/`, persistence tests, documentation, scene generation, integration/runtime verification.

## Shared contracts (freeze before parallel work)

All Core types namespace `LingGuangV05.Core`; Unity-independent assembly `LingGuangV05.Core`.

`GameState`: serializable public fields `int version, chapter, day, gpuCount, caseCount, semanticLevel, trainingLevel, cardsReviewed, correctedCards, examAttempts, examAnswered, examCorrect`; `double money, samples, steps, learningPoints, gameSeconds, daySeconds, billDue, totalEarned, temperature`; `long lastSeenUnix`; `bool jobEnabled, breakerTripped, unpaidPower, examActive, chapterOneComplete`; `string aiName`; `List<NodeState> nodes`; `List<EdgeState> edges`; `List<string> mistakes`; other fields allowed for persistence/determinism. Initialize every collection. Active question/RNG/exam progress MUST survive save/load.

`NodeKind { Input, Hidden, Yes, No }`; `NodeState`: `int id,q,r`; `NodeKind kind`; `double charge,fatigue`.
`EdgeState`: `int from,to`; `double weight`.
`CardData`: `int id`; `string prompt, explanation, category`; `bool expectedYes,predictedYes`; `double confidence`.
`CardResolution`: `int cardId`; `bool correct,corrected`; `string explanation`.
`TrainingPulse`: `int[] nodeIds`; `bool correction`.
`GameConfig`: defaults configurable; `double dayLengthSeconds=120, gpuPrice=450, casePrice=2000, electricityPrice=.52`; `int examQuestions=20, examPassScore=16` plus own tunables.

`ChapterOneSim(GameState state=null, GameConfig config=null)`; properties `GameState S`, `GameConfig Config`, `CardData CurrentCard`; events `Action Changed`, `Action<CardResolution> CardResolved`, `Action<TrainingPulse> Pulsed`.
Computed: `double Accuracy, HeartbeatsPerSecond, PowerWatts, MemoryUsed, MemoryCapacity, IncomePerSecond`; `bool HasOutputPath, CanStartExam`; `string Bottleneck, LastMessage`; `int FanOutCount, ConvergeCount`.
Commands returning bool on accepted action: `SubmitCard(int cardId,bool playerAnswer)`, `BuyGpu()`, `SellGpu()`, `BuyCase()`, `PayBill()`, `ResetBreaker()`, `UpgradeSkill()`, `UpgradeTraining()`, `AddNode(int q,int r)`, `RemoveNode(int id)`, `ToggleEdge(int from,int to)`, `StartExam()`.
Other: `void Tick(double seconds)`, `void SetJobEnabled(bool enabled)`.
Use q=0..6,r=0..4 board with protected Input/Yes/No ports. Tests define its precise hex rules; UI calls commands, never mutates state. Invalid/replayed commands must not grant resources. Chapter one finite content includes 20-email entrance test, failure/retry, one-time reward; completion is explicitly marked, chapter two is not implemented.

`LingGuangV05.Runtime.ChapterOneRuntime : MonoBehaviour`, assembly `LingGuangV05.Runtime` references Core + Unity. Parent creates this OUTSIDE all window objects. `public ChapterOneSim Sim {get;}`; `public event Action Changed`; `public string SaveStatus {get;}`; `public string SavePath {get;}`; `public void EnsureInitialized()`, `public bool SaveNow()`, `public bool ReloadSave()`, `public bool ResetProgress()` (UI must require confirmation), `public bool RecordName(string name)`; serialized `bool useDiskSave=true`, `bool tickEnabled=true`. Starts before ordinary MonoBehaviours. Owns ticking, save/load/version checks/atomic write/backup and bounded offline accounting. No references to UI/DreamOS. Supports injected in-memory simulation via `public void SetSimulationForTests(ChapterOneSim sim)`.

`LingGuangV05.UI.ChapterOneApp : MonoBehaviour`, assembly `LingGuangV05.UI` references Core, Runtime, Unity.TextMeshPro, Unity.ugui. `public void Initialize(ChapterOneRuntime runtime, RectTransform content, TMP_FontAsset font)` builds app under given content. `public void ShowTab(string tab)` accepts cards/board/skills/shop/jobs/power/persona/exam/chat. `public void Refresh()`. All generated GameObjects layer 5, use existing source canvas, no cameras/event systems/global input. Pointer handlers only. Stable action names for UI QA. Window size ~1250x760 at 1920x1080 source. Tabs must have actual chapter-one functionality; later chapter-only tabs clearly locked with reasons. Beginner instructions, card feedback, board selection/connect mode, hardware slots, bills, simulated confidence clearly labeled. Performance: refresh on changes throttled, reuse controls, bounded text/log.

`LingGuangV05.Desktop.ChapterOneDesktopBridge : MonoBehaviour` is un-asmdefed adapter. Parent/runtime outside window; serialized root references. Public `OpenApp(string tab="cards")`, `CloseApp()`, `MinimizeApp()`, and expose app/window/runtime for QA. Desktop icon + taskbar reopening + existing Aero pointer routing must work. Window controls use existing DreamOS WindowManager if practical, preserve window dragging/maximize/minimize. Existing other apps stay functional. First-run Lao Zhou intro can be shown inside the game's chat tab/toast without altering vendor messaging persistence.

Editor installer: `LingGuangV05.Editor.ChapterOneInstaller.Install()` plus menu `Tools/LingGuang/Install Chapter One Desktop`. Assert active scene clean; duplicate original 2010desktop through Editor API to `Assets/LingGuangV05/Scenes/Main.unity`, work only on copy, inject runtime/window/bridge; save new scene; do not change global build list/render settings or vendor assets. Idempotent on owned scene. Builder and adapter exclusively owned by desktop worker; coordinate app/runtime signatures with other workers.

## Acceptance / plan
- [ ] RED core tests before implementation, then GREEN: card judgment, correction, deterministic save continuity, topology/weight/fatigue, resource caps, jobs, GPU/case, thermal/electricity/breaker/debt, daily boundary, test pass/fail/retry/idempotent reward.
- [ ] Runtime: one ticking authority independent of window lifetime; versioned atomic save with backup, malformed-save recovery, offline time bounded and UTC rollback safe; no writes to old game data.
- [ ] Desktop/UI: playable 9-tab shell, first chapter interactions, all text in Chinese, new-player flow from Lao Zhou to first contract and first exam. Later chapters locked honestly.
- [ ] Unity import/compile -> scoped EditMode suite -> actual Main Play Mode UI/desktop/input regression -> screenshot -> preserve hashes.
- [ ] Run repeatable first-chapter walkthrough and save/reload/closed-window checks; record what is and is not proven. No player-build claim without build.

## Design rulings
- First chapter uses rules/seeded simulated answers. Real models are out of this approved phase.
- Intro exam is 20 spam emails, pass at 16; gate and economy tuning must live in GameConfig. Retry does not clear progress; first success reward only once.
- Existing LCD/1920x1080 shell stays; 2010 visual style is not a request to reintroduce CRT distortion.
- Prioritize a complete playable loop, not placeholder screens or empty event bus abstractions.
