# First-chapter desktop apps — implementation / QA handoff

Owned files: this UI folder only. No Editor, scene, package, vendor, Git or old prototype changes were made by the UI worker.

## Entry points

`ChapterOneApp.Initialize(ChapterOneRuntime runtime, RectTransform content, TMP_FontAsset font)` is safe before Awake/Start. It builds only once, under `content`, and supports re-binding the runtime with the same content. All created objects use layer 5. It uses the caller's existing Canvas / EventSystem; no cameras or global input reads.

- Call `ConfigureProfile(string appId)` **before Initialize**. Unknown IDs throw; once built, reapplying the same ID is harmless but changing to another profile throws. Default is `lingguang`.
- `ProfileId`, `ProfileTitle`, and defensive-copy `AvailableTabs` expose the profile. Only these pages and their controls are built, not hidden copies of nine pages.

| App ID | App title | Exact available pages | Presentation |
| --- | --- | --- | --- |
| lingguang | 灵光.exe | board, skills, persona, exam | Model/network workbench |
| mail | 邮件 | cards | Folder sidebar, current-message list, sender/subject/body reader, classification |
| xunbao | 寻宝 | shop | Orange/blue local hardware marketplace |
| home | 家庭 | jobs, power | Household income and electricity management |
| yy | YY | chat | Lao Zhou contact, dialogue bubble, preset reply chips |

- `ShowTab(string)` only accepts the current profile's built pages. Cross-app or unknown routes are ignored.
- `Refresh()` — explicit immediate UI sync; Update otherwise coalesces dirty runtime events to 4 Hz.
- `SubmitAnswer(bool)` — submits the currently **displayed** card; same-frame duplicates ignored. Mail synchronously refuses while an exam is active, even before its next refresh. LingGuang only submits from its active exam page during an exam.
- `SetBoardMode(string)` — `connect`, `place`, `remove`.
- `ClickBoardCell(int q, int r)` — same path as actual UI clicks.
- `ShowHelp(string)` — `intro`, `board`, `money`, `exam`; operates only in YY.
- Read-only QA properties: `UiRoot`, `ActiveTab`, `BoardMode`, `SelectedNode`.
- `EffectiveFont` returns the app-owned dynamic CJK TMP asset; caller must not destroy it. `ApplyEffectiveFont(TMP_Text)` is preferred for scene-local chrome, because it remembers/restores the prior font reference when the app is destroyed.

The app never ticks or directly mutates the simulation. Runtime replacement from load/reset is detected and Core event subscriptions are rebound. Hiding the window does not disable the external runtime. No Refresh rebuilds controls or resets keyboard focus. Renaming retains unsaved input through refreshes; all dynamic text has TMP rich text disabled, and naming goes through `Runtime.RecordName`.

Each view binds the **same** runtime; there are no per-app economy or save copies. App-local helper methods cannot place/edit a board from foreign profiles. The UI does not operate the desktop router itself; guidance names the software to open.

## Font ownership

The desktop's static SparkUI CJK asset was found incomplete during parent Play-mode QA. The UI now loads the existing project `Resources/Fonts/NotoSansCJKsc-Regular` Font and creates its own dynamic TMP asset at runtime (48pt / 5px padding / SDFAA / 1024² / multi-atlas). It prewarms a bounded finite UI corpus, while new cards and names populate dynamically. It does not change font importers, source font, passed desktop TMP asset, global fallbacks or TMP Settings. If the source resource is missing, it logs a warning and falls back to the passed asset. On app destruction it restores registered external consumers, destroys owned UI, then releases every owned atlas texture, material and font with a short delay so label destruction happens first. No disposal on minimize/close.

## Stable GameObject / button names

- Tabs: `Tab_cards`, `Tab_board`, `Tab_skills`, `Tab_shop`, `Tab_jobs`, `Tab_power`, `Tab_persona`, `Tab_exam`, `Tab_chat`.
- Mail: `CardAnswerYes`, `CardAnswerNo`, `MailOpenCurrent`; prompt `CardPrompt`, subject `CardTitle`, sender `MailSender`, list `MailMessageList`, reader `MailMessageReader`, simulated suggestion `CardSuggestion`, explanation `CardFeedback`.
- Board: `BoardMode_connect`, `BoardMode_place`, `BoardMode_remove`; cells `BoardCell_0_0` through `BoardCell_6_4`; `WeightedEdges`, `BoardStats`, `BoardSelection`.
- Skills: `UpgradeSkill`, `UpgradeTraining`.
- Hardware: `BuyGpu`, `SellGpu`, `BuyCase`.
- Work: `ToggleJob`.
- Power: `PayBill`, `ResetBreaker`.
- Identity: `AiNameInput`, `RecordName`.
- Persistence: `SaveNow`, `ReloadSave` exist in each app and affect shared state. `OpenResetConfirmation`, `CancelReset`, `ConfirmReset` and modal `ResetConfirmationOverlay` exist only in LingGuang.
- Exam: `StartExam`, `ExamCardAnswerYes`, `ExamCardAnswerNo`; `ExamSummary`, `ExamCardPrompt`, `ExamCardFeedback`.
- Local help: `ChatHelp_intro`, `ChatHelp_board`, `ChatHelp_money`, `ChatHelp_exam`.
- Global feedback: `Objective`, `FooterStatus`, `SaveStatus`, `HeaderState`.

## Repeatable Play-mode checks for parent

Use an injected in-memory Runtime for destructive/failure fixtures. The actual desktop click flow must also be tested separately; calling methods or `Button.onClick.Invoke()` does not prove pointer routing.

1. Open each of the five desktop shortcuts; confirm exact page grouping, role-specific presentation, titles, Chinese font and no hidden foreign pages. Reuse the native Mail/YY shells without duplicate entries. Test normal, maximized and restored window sizes.
2. Correct/incorrect card choice: use expected fixture card answers, confirm resources change only for correct labels; new card appears immediately; explanation remains from the previous card. Call twice in one frame and assert only one card is consumed.
3. Training suggestions must say “模拟模型判断 / 模拟置信度”; no expected answer is leaked. Starting the LingGuang exam pauses Mail classification immediately: stale Mail click cannot advance the exam, buttons become disabled and inbox tells the user to return to LingGuang. Once the exam ends, Mail resumes training cards.
4. Board starts with 35 axial cells, 8 nodes and 7 directed edges. Place at (1,1); duplicate place reports rejection. Remove protected Input at (0,2): rejection. Connect an adjacent pair; repeat to remove. Nonadjacent connection must report Core's exact reason and retain selected origin.
5. Observe slowly fading purple correction feedback and normal teal pulse, weight-dependent thickness, no flash effect. Node labels stay readable; selection persists until completed/cancelled.
6. Skill/training purchases: show exact configured cost, successful deduction once, insufficient points visibly rejected, maximum level disabled.
7. GPU buy/sell and case purchase: show exact current costs, slots, memory, power; insufficient cash or forbidden memory reduction shows rejection. Case capacity comes from `gpusPerCase`.
8. Work: enable before three reviewed cards -> status explains gate, ¥0/s; after gate -> actual income visible. Disable -> income stops but training remains live.
9. Power: day-boundary automatic deduction, insufficient funds outage, manual labels repay debt without cash; over-limit breaker rejects reset until GPU load is reduced. High temperature is described as throttling, not a fictional thermal trip.
10. Rename valid CJK name; reject `<b>name</b>`, blank/control input and >16 text elements through Runtime. Refresh must not discard unsaved name drafts.
11. Save/reload preserves active card/exam progress and topology. Opening reset dialog must not change state; cancel preserves progress. Modal blocks underlying buttons and focuses safe cancellation; only confirmation calls ResetProgress.
12. Exam: missing prerequisites return exact reason, 20-question progress, failure retained with retry option, success shows chapter-one completion and locks StartExam. Future content explicitly remains unimplemented.
13. Close/minimize any or all apps, advance runtime, reopen: shared resources/temperature/day progressed and views did not rebuild. No duplicate event listeners after reinitialization or load/reset. A purchase in 寻宝 must immediately affect 灵光/家庭 after refresh.
14. Inspect console for UI errors and check that every visible click routes through the existing desktop's UI input module.

## Verification performed by UI worker

- Compiled Core + Runtime + UI linked sources using .NET SDK 10.0.301 against this project's Unity 6000.4.5f1 managed modules, UnityEngine.UI.dll and Unity.TextMeshPro.dll: **0 errors, 0 warnings**.
- This is C# source/API validation, **not** Unity import, EditMode, Play-mode, screenshot, actual pointer-input, or player-build proof. Parent owns and must execute those verification steps.
- Layout is intended for the desktop-provided content rectangle ~1236×693, with bounded read-only labels and no scrolling/high-frequency animation. Smaller native windows need visual QA.

## Regressions discovered by parent Play QA and corrected

1. Custom board `MaskableGraphic` objects lacked `CanvasRenderer`, causing `GraphicRaycaster` exceptions. Both graphic classes now explicitly require it, and the builder creates it before adding each custom graphic.
2. Noto CJK's line box is ~1.45 em, so Ellipsis mode suppressed entire short title/counter rectangles. Fixed-position Label creation now reserves the actual font-metric line height plus 2 px once, and header rows have explicit non-overlapping positions. No font scale change and no live-counter autosizing.

`UI/Tests/Editor/LingGuangV05.UI.Tests.asmdef` retains renderer/layer and title line-box regressions, adds a Mail-specific title check, five exact-profile/build/navigation cases, profile validation, and a two-view shared-runtime Mail/exam isolation test including post-exam resumption. Test source compiles with the local Unity/NUnit assemblies. Execution remains parent-owned; source compilation is not an assertion that these Editor tests passed.

## Complexity / boundaries

- View refresh: O(V² + E) due to 35-cell linear node lookup; V≤35 by design, E bounded by axial adjacency. No unbounded UI creation after Initialize. Board mesh O(E), one mesh rather than an object per edge.
- UI storage: O(V + E + fixed controls); last card explanation and current help message only, no growing conversation log.
- Dynamic numeric formatting still allocates bounded strings at 4 Hz; no zero-GC claim. Simulation, save safety, economics and content correctness remain Core/Runtime responsibilities.
