# User-approved application split — 2026-10-03

The user now explicitly requests distinct desktop software instead of nine tabs in one app. Preserve original 2010desktop; only update the already-created `Assets/LingGuangV05/Scenes/Main.unity` copy. Keep the existing first-chapter simulation/persistence unchanged.

## Exact mapping
1. **邮件**: cards/data labeling. Reuse Main scene's existing DreamOS Mail window, taskbar entry and desktop icon. Present email sender/subject/body and classification actions, not another nine-tab game dashboard.
2. **灵光.exe**: board, skills, persona, exam ONLY. Existing chapter app window retained.
3. **寻宝**: shop ONLY. Standalone shopping app evoking Taobao, but no real logos/network. New scene-local window/icon/taskbar/start menu entry.
4. **家庭**: jobs and power ONLY. Standalone household management app with these two pages.
5. **YY**: chat/Lao Zhou only. Reuse Main scene's existing DreamOS Messaging window, taskbar entry and desktop icon. Contact/conversation style evoking QQ. No free-form real LLM implied.

## Ownership
- UI agent: only `Assets/LingGuangV05/UI/**`. Add app-profile configuration, filtered navigation/build, per-app titles/presentation, emails and YY style, updates to UI tests.
- Desktop agent: only `DesktopBridge/**` + `Editor/ChapterOneInstaller.cs` (NOT parent-owned ChapterOnePlayQa.cs). Add route registry/adapter bindings, reuse Mail/Messaging windows and actual existing icon/taskbar entries, clone new shop/home windows, handle safe migration from the installed single-app scene. Fix new shortcut compatibility with native SettingsManager (see below). Builder code only; parent operates Editor.
- Parent: Runtime/persistence unchanged except needed tested fixes; owns actual scene, Play QA, evidence, docs and preservation checks.
- Explorer: read-only Mail/Messaging/desktop contracts reconnaissance; report reusable scene native component APIs, no edits/Editor writes.

## Shared API extension
`ChapterOneApp.ConfigureProfile(string appId)` MUST run before Initialize; accepted ids `lingguang`, `mail`, `xunbao`, `home`, `yy`. Default is `lingguang`. `public string ProfileId {get;}` and `public string[] AvailableTabs {get;}`. Valid tabs: lingguang=[board,skills,persona,exam], mail=[cards], xunbao=[shop], home=[jobs,power], yy=[chat]. ShowTab cannot route to another application's page. Each app has its own independent view instance but all bind the SAME ChapterOneRuntime. Keep dynamic-font fixes, CanvasRenderer regressions, pointer-only interaction, font ownership and UI QA stable names.

Desktop bridge gains serialized/public ApplicationId and configures profile before app.Initialize. Preserve existing properties OpenApp(tab), App, Window, Runtime, shortcuts. A new `ChapterOneDesktopRouter` on runtime root holds/exposes bindings, `Get(string appId)`, `Open(string appId,string tab=null)`, `Bindings` collection. IDs above. `OpenApp` defaults to profile's first tab (not always cards). Explicit route uses the correct app; native reopen preserves last tab.

Reused Mail/Messaging native windows/icons must remain one entry each, retitled only on this scene copy. Do not modify shared AppLibrary/UIManager assets; create scene-local/owned library copies if required. Original demo content can be disabled/replaced under a retained scene-local Content container, but do not delete unrelated third-party assets/apps. Mail and YY styling should use their actual app roles and original chrome.

## Known native integration issue to fix
New desktop shortcuts lacking `Michsky.DreamOS.ItemDragger` cause `SettingsManager.SaveDesktopOrder()` to null-reference while enumerating Desktop List children. Add/configure expected ItemDragger on owned shortcuts (or use native icon template); NEVER patch vendor code globally. Check related native list assumptions. This appeared on a later Play startup, even though the initial walkthrough passed; include regression in split-app startup checks.

## Acceptance
- One runtime, five functional app windows with exact grouping, correct labels/icons/taskbar/start menu, no duplicate Mail/Messaging.
- No money/state fork when changing apps; closing ANY or ALL windows never pauses training/jobs.
- Complete first loop across Mail → LingGuang → Household → Xunbao → LingGuang exam; YY help accessible.
- CJK text and caption glyphs visible, custom graphics have renderers, resize/drag/focus works through existing LCD input relay.
- Persistence tests stay green; original/protected asset hashes and pre-existing tracked diff unchanged.
- Main scene saved with useDiskSave=true for real use; QA runs in memory to avoid polluting player data.
- Preserve earlier regression evidence, produce new app-split screenshots and complete Play report. No Player build claim.
