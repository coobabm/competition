# Work ledger — 2026-10-03

## Current user-approved direction
- Keep original `2010desktop.unity` untouched; all integration in copied `Assets/LingGuangV05/Scenes/Main.unity`.
- Five apps: Mail=data labeling; LingGuang=board/skills/persona/exam; Xunbao=shop; Home=power bills/jobs; YY=Lao Zhou native Messaging.
- User rejected generic selfdraw dark interiors. YY was restored from the original native Messaging component tree. User subsequently requested LingGuang/Xunbao/Home use native controls with distinct hightech/marketplace/bill-first content and generated art.

## Verified milestones
- Original scene copied byte-identically before modification; new GUID 92f30303240d44d4589e5a9f6b122482.
- First chapter Core, economic/board/card/exam flow, atomic persistence implemented. Earlier monolith had 64 EditMode and65 Play checks; these are historical, not proof of latest design.
- App split preserved native Mail/Messaging root+taskbar IDs and16 totalwindows; latest Mailtitle newline fix was included in subsequent80-test passing run.
- YY actualnative restore succeeded: native MessagingManager, WindowPanelManager, original hierarchy; App=null/customdescendants0; old Messagingroot ID419237029 retained; reference sceneclosedunsaved andsourceSHAunchanged.
- NativeYY actualLCD-relay21checks passed (send,delayedreply,close/reopen,emptyinput). Screenshot native-yy-restored.png confirmslightoriginallayout.
- Reviewer found TMPborrowedfontcloneOnDestroy P1. Parentdetached2livecloneatlas/material beforeStop. Workerfixedcleanup+addedUnityregression+replytimeusesManager.GetTimeData. These fixesneedfinalfreshUnityverification.
- FourImageGen PNGs generated, copiedandvisuallyinspected; metadataalpha confirmed. Files underArt/Generated; prompts.md; generated-art.sha256.

## In-progress ownership
- chapter_desktop: nativeAppsInstaller actual3layouts usingnativeButtonManager/Input/panels, Bridge.NativeApp/IsNativeChat andoldinstallerpreservationguards. NoEditoroperations.
- chapter_ui: NativeAppView bindingcontrollerandNativeAppFontFallback; onlyboarddiagramconstructsgraphics. NoEditoroperations.
- Parent: NativeAppBindings contract, actualimport/sceneinstall, testjobs, QA,screenshots,preservation, docs.
- Reviewer: readonlyresourceownership/controlintegrationreview.

## Required finishing work
1. WaitstableNativeAppsInstaller+NativeAppView files; compileUnityandverifylatestassemblies (testtoolcanrunstaleassemblieswhencompilererrors).
2. Runall3asmdefEditModesuites includingnativefontdisposalregression.
3. Execute ChapterOneNativeAppsInstaller.Install on Main; confirmall4imagesSpriteswired, NativeApp on3bindings, NativeChatYYunchanged, Maillegacycustominthispassunchanged.
4. Update/run ChapterOneSplitPlayQa (parentalreadyaddedNativeApp/NativeChatbranches) acrossapps; run NativeChatQa againaftercleanupfix; inspecteachappactualscreenshot,fixlayout/contrast/nativecontrolissues.
5. FinalsourcechecksandMainuseDiskSave=true saved inEditMode. ProjecttrackedpreexistingdiffmustmatchEvidence/preexisting-tracked.diff; originalscene/Aero sourcechecksums Evidence/protected-before.sha256.
6. Documentrealboundaries: localrulemodel,noQwen/noPlayerbuild; newP1fontfixmustnotbeclaimedverifiedbeforeUnitytest.

## Existing evidence
`Evidence/LingGuang-ChapterOne/`: baseline tracked diff/protected SHA; initial/finalmonolithtestjson; nativerestorejson; nativeappsbefore/afterIDs; nativechat21QAjson/screenshot; generatedartSHA; backupsMain-before-app-split.unity andMain-before-native-chat.unity outsideAssets.

MaincurrentdiskSaveflagmayhavebeenleftfalsebytestfixtures; explicitlyrestoretrueatend. OnlyEditorparentoperatesUnity. NoGitcommits/pushes/globalsettingschangesauthorizedorperformed.
