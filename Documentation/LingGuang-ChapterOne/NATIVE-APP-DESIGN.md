# Native component application design — user refined direction 2026-10-03

User: 灵光、寻宝、家庭也要原生风格；灵光高科技设计感，寻宝类似淘宝，家庭主要账单；能用图片的地方生成图片并配好。

Keep native Aero windows/controls. Use actual DreamOS ButtonManager/Input/Switch/etc prefabs or scene instances, native fonts/spacing/control shapes. Distinct app content themes are now explicitly requested: technology for LingGuang, marketplace for Xunbao, bills for Home. Do not reintroduce selfdraw YY; restored Messaging remains native. Mail is unchanged in this pass. Existing Core+Runtime/persistence remain authoritative.

## Ownership
- Native controller worker: `DesktopBridge/ChapterOneNativeAppView.cs` and optional `NativeAppFontFallback.cs` only. No scene tools. Bind existing Core commands to native controls; build only interactive neural diagram cells/edges (game content, not new UI skin).
- Native layout worker: `Editor/ChapterOneNativeAppsInstaller.cs` plus changes to DesktopBridge/Router/old installers for native app support. No scene tools. Construct three interfaces using native control/template instances; no raw bespoke UI button/shape renderer. Only parent runs installer. Does NOT own NativeAppBindings.cs or controllers.
- Art worker: `Art/Generated/**` only, built-in ImageGen four assets.
- Parent: contracts, integration, scene, QA, documentation.

## Existing shared types
`NativeAppBindings.cs` already exists in Assembly-CSharp and defines serializable arrays texts/buttons/inputs/images/pages. Native buttons are `Michsky.DreamOS.ButtonManager`; text TMP_Text; input TMP_InputField; images UnityEngine.UI.Image. Empty layout RectTransforms are fine; visible controls should be cloned/reused native widgets. Parent will import generated PNGs as Sprite if needed.

## Controller API
`ChapterOneNativeAppView : MonoBehaviour` namespace LingGuangV05.Desktop, no asmdef.
`Configure(ChapterOneRuntime runtime,string profile,NativeAppBindings bindings)`; `EnsureInitialized()`, `ShowTab(string)`, `Refresh()`.
Properties `ProfileId`, `ActiveTab`, `string[] AvailableTabs`, `IsInitialized`, `Bindings`, `Runtime`.
`FindControl(string id) -> RectTransform` supports native buttons and generated BoardCell_q_r; `SubmitAnswer(bool)` for native exam.
Native controller never draws main interface; only binds builder-provided controls. Subscribe to Runtime.Changed and current Sim for pulse, detach appropriately. Preserve guarded commands, current card ID and no duplicate submission per frame. CJK fallback must be per-instance, keep original sizes/materials; when shallow font clones borrow atlas/material, detach those references before destroying clones (TMP destroys references in its own destructor).

Bridge adds serialized nativeApp and public NativeApp. `IsNative` can be true for nativeChat OR nativeApp; add `IsNativeChat` for disambiguation. Keep App=null for these3 and YY. ConfigureNativeApp(...) supplies runtime, view, window, content and existing launch/caption refs. EnsureApp/OpenApp branches call nativeView methods, not ChapterOneApp. Old installers preserve native bindings; new installer idempotently updates only3 apps. Original Main window identities/icons/taskbar retained.

## Control/page IDs and role-specific layout
Common text IDs: Brand, HeaderMoney, HeaderSamples, HeaderSteps, HeaderLearning, Objective, Status, SaveStatus. Common native buttons SaveNow, ReloadSave; latter may require confirmation before discarding unsaved changes.

LingGuang pages `[board,skills,persona,exam]`. Native segmented navigation + high-tech dark-blue/cyan content, generated neural-workbench artwork as hero/header, information-rich but readable. Diagram area dominates board page; board is interactive not an image replacement.
- board: BoardMode_connect/place/remove native buttons; BoardStats, BoardSelection text; bindings.boardArea supplied. Reuse existing HexCellGraphic/BoardEdgesGraphic, protectedports/adjacent edges and existing state; all graphics have CanvasRenderer, layer5, mapped pointer events only.
- skills: UpgradeSkill, UpgradeTraining buttons; SemanticStats, TrainingStats text.
- persona: NameInput input; RecordName; PersonaStats; show current saved AI name, no fictional LLM behavior.
- exam: StartExam, ExamCardAnswerYes, ExamCardAnswerNo; ExamSummary, ExamPrompt, ExamSuggestion, ExamFeedback text. Disabled answers when no active exam.

Xunbao only `[shop]`: marketplace white cards, orange highlights, search input, category/brand-like header (original寻宝, norealtrademark), generated GPU/case product images large and actually wired. Product prices/stock/slots clear, strong native purchase CTA and secondary sell.
- native buttons BuyGpu, SellGpu, BuyCase. Texts GpuPrice, CasePrice, GpuStats, CaseStats, ShopSummary. Images GpuArt, CaseArt. SearchInput optional: filter product cards `ProductGpu`, `ProductCase` (bind as pages? do NOT use page IDs forproducts; controller may locate byname). Keep both visible on blank search, filter by 显卡/GPU/机箱/电脑 etc.

Home pages `[power,jobs]` (bills first): light receipt/statement design using native panels/rows, dominant amount due/current day, actual kWh/rate/device cost lines, generated household-meter illustration. No fabricated historical paid bills.
- power: PayBill, ResetBreaker buttons; BillAmount, BillDetails, PowerStats, BillStatus text; image MeterArt.
- jobs: ToggleJob button; JobStats, JobStatus text. Secondary income panel links economically to bills.

## Art paths
- Art/Generated/gpu-9800.png (already ready,1254squarealpha)
- Art/Generated/pc-tower.png (already ready,1254squarealpha)
- Art/Generated/neural-workbench.png (ready,1774x887opaque)
- Art/Generated/household-meter.png (generation underway)
Paths relative to Assets/LingGuangV05. Do not fake missing images; defer wiring until present. Newinstaller should load/import realfiles and assign serializedImage.sprite with correct aspect; parent verifies screenshots.

## Native template hints
Read existing Aero2010 UI/theme references, notrawvendor darkdefaults. Native prefab paths include DreamOS.../Prefabs/UI Elements/Button/Default Button.prefab, Navbar Button.prefab, Input Field/Input Field.prefab, Settings/Settings Element - Button.prefab, Settings Element - Switch.prefab, Game Hub/Library Item.prefab. Actual Aero contrast copies live Assets/HongmengOS/Aero2010/Theme/Prefabs/Library Item_*.prefab etc. Assign scene-local/Aero theme; do not modify shared originals. Theme cloning/tinting for explicitly requested per-app identity is okay, but retain real native components/shapes/interaction.

## Acceptance
Three visibly distinct software interfaces, originalnativechrome and genuine nativecontrols, generatedart loadedfromprojectpaths. First-chapter commands remain functional; Mail→Homejob→XunbaoGPU→LingGuangboard/skills/exam still works. YY remains originalnativechat. No originalscene/vendorassets/globalsettings touched. Tests + physicalLCDrelayclicks + screenshots afteractualimport; noPlayerclaim.
