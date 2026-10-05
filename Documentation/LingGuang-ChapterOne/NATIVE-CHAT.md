# Native YY restoration — explicit user instruction 2026-10-03

User: 把聊天软件弄回来 回到原生组件上接功能 不要自绘。

Restore YY from the pristine `2010desktop` native Messaging subtree. No self-drawn panels, dashboard, contacts, bubbles or input controls. Preserve native layout, prefabs, materials, font sizes, chrome, animations and interactions; only data bindings/content and necessary CJK fallback may differ. Work on Main copy only. Other apps are not to be restyled while this targeted restoration is underway.

## Ownership / contracts
- Desktop worker: new `Editor/ChapterOneNativeChatInstaller.cs`; changes to `DesktopBridge/ChapterOneDesktopBridge.cs`, `ChapterOneDesktopRouter.cs`, and old `Editor/ChapterOneInstaller.cs` ONLY to support native chat and prevent future destructive reapplication. No Editor operations, no scene YAML edits.
- Native adapter worker: new `DesktopBridge/ChapterOneNativeChatAdapter.cs`, `Runtime/NativeLaoZhouReplies.cs`, `Tests/PersistenceEditor/NativeLaoZhouTests.cs` only. No UI construction or style/color/fontsize change. No Editor operations.
- Parent: operates live Unity, restores/saves Main, validates native subtree/ref wiring/send/receive/scroll/focus, maintains docs and evidence.

`ChapterOneNativeChatAdapter` namespace `LingGuangV05.Desktop`, Assembly-CSharp (no asmdef). Serialized public properties/fields `MessagingManager Manager`, `ChapterOneRuntime Runtime`, `WindowManager Window`. Methods `EnsureInitialized()`, `OpenConversation()`, property `IsReady`. Uses original manager `externalEvents` after send, native `CreateIndividualMessage`/`CreateCustomIndividualMessage` to reply; no custom view. Guard reply recursion, empty input, bounded length and pending reply queue; runs on a holder outside window so closing window does not lose a reply. First chapter local scripted Lao Zhou help is explicitly not an LLM.

Bridge serialized `nativeChat` and public `NativeChat` getter. Native binding may have App=null; `IsNative` bool identifies it. Add `ConfigureNative(ChapterOneRuntime runtime, WindowManager window, ChapterOneNativeChatAdapter chat, Button shortcut, TaskbarButton taskbar, TMP_Text[] labels=null)` or equivalent; preserve existing public launch/close/minimize APIs and Router id `yy`. EnsureApp/OpenApp branches must not create ChapterOneApp or require custom content/font for native chat. Native opening uses WindowManager.OpenWindow then native conversation; native titlebar buttons remain original event wiring.

New installer `ChapterOneNativeChatInstaller.Install()` menu `Tools/LingGuang/Restore Native YY`. It opens original 2010desktop additively for read-only reference, restores Main's existing Messaging GO in place (preserve original root/window/taskbar identity and remap source scene-object refs to Main) or provides equivalent safe restoration. Close reference scene without saving. Do not restore modified custom subtree from current Main. Use native MessagingManager, WindowPanelManager, original Container/Content/controls and native message/contact prefabs; remove the self-drawn ChapterOneApp from YY.

Prefer preserve existing root WindowManager and GO IDs. Recreate original child hierarchy/components using Unity APIs, remap cross-child and external scene references. Main's source canvas/user/system/taskbar must be referenced, not reference-scene objects after additive source close. Preserve visible app name YY at scene-local labels only. Native taskbar/icon can retain installed scene-local launch handler; both must route to restored original window.

Create data-only native `MessagingChat` asset under `Assets/LingGuangV05/Content/Native/` with Lao Zhou intro. Use original MessagingChat/ChatItem mechanisms; set useLocalization=false and saveMessageHistory=false to avoid changing existing DreamOS user history. Existing demo assets never edited. A single Lao Zhou contact is adequate for this phase; no demo actors need to become game characters.

Use per-instance fallback fonts only when needed for Chinese. Do not alter native color/spacing/fontsize or mutate shared font/prefab assets. Native input and spawned ChatMessagePreset content should treat user text as plain text; no commands/network/model calls.

## Verification
- Original source hash unchanged; Main's native Messaging manager/layout/buttons/input restored; no ChapterOneApp descendants, no self-drawn chat UI root.
- Native contact selected, intro renders, typing and original send button create self bubble, delayed Lao Zhou response creates received bubble; reopen preserves runtime conversation and game simulation continues.
- Screenshot visually compared to original native Messaging; native list, whitespace, light Aero style and input toolbar preserved.
- No missing scripts, external references to closed source scene, new console exceptions or original asset changes.
- Existing 5-app router entry YY still launches; installer idempotence cannot reintroduce custom YY.
