# Packaging license/source audit — 2026-10-20

Scope: read-only source audit of `competition-integration-20261020` against the older `resume-20261019/qwen35/package-2026.10.19.5/Package-VerifiedRelease.ps1`. No Assets changes, Unity invocation, uploads, commits, or restoration of removed paid packs. This is a packaging recommendation, not a final Player dependency audit; no final Unity BuildReport was inspected.

## Required packaging changes

| Existing entry | Recommendation | Evidence / retained obligation |
| --- | --- | --- |
| `Assets/Feel/NiceVibrations/Demo/Common/Fonts/RobotoMono-Medium.ttf` | MOVE source check to `Assets/LingGuangV05/Localization/Fonts/RobotoMono-Medium.ttf`; retain exact hash validation | Same 109292-byte font, SHA256 `8E3FC643B30CFF29C10E5F57534D2FFCF791F1319DFF30C3E37C2B6B5E3D679F`. OriginMono SDF still references source GUID `87930fbd85b3f4b56b6d709cfca95c31`. |
| Companion `RobotoMono-NOTICE.txt` | KEEP and update its old source path | Embedded copyright is `Copyright 2015 Google Inc. All Rights Reserved.`; trademark `Roboto Mono is a trademark of Google.`; Apache License 2.0. Retain full `Apache-2.0.txt` alongside the provenance note. |
| `Assets/DreamOS - Complete OS UI/Read Me.txt` | DROP obsolete required path | Removed pack. Current OS art/audio regeneration documentation replaces the resource-level basis for the old acknowledgment list. Do not restore the pack. |
| `Assets/Reach - Complete Sci-Fi UI/Read Me.txt` | DROP obsolete required path | Removed pack; no exact old Reach non-meta asset blob match found in the comparison. Do not restore it. |
| `Assets/HongmengOS/Licenses/Lucide-LICENSE.txt` | DROP obsolete runtime required path | Current generated icons do not match archived Lucide replacements; archival license outside Assets can remain with the archive. |
| Open Sans paragraph in `Fonts/FONT-DEPENDENCIES.txt` | KEEP Apache attribution | The retained OpenSans-Bold.ttf binary explicitly names Apache License 2.0. New upstream prose calling it OFL does not describe these exact bytes correctly. |
| Lucida Console / LCR.ttf paragraph and statement that DreamOS/Reach acknowledgments accompany it | DROP stale bundled-font claim, subject to final Player inventory | No LCR.ttf or Lucida family asset found in current inventory. Do not imply Apache/OFL covers Lucida. |
| Hardcoded `2026.10.19.5` in package README | UPDATE to verified build result version, expected `2026.10.20.1` | Keep release statements aligned with actual completed validation and actual archive assets. |

`Assets/LingGuangV05/Localization/Fonts/RobotoMono-LICENSE.txt` is only a four-line attribution and is not the complete Apache license. Its copyright wording also differs from the font's embedded name table. It does not replace the full Apache text and exact companion attribution already provided by the old packager. The old packager's hash-validated complete Apache text can be retained.

## Open Sans evidence

Current source is `Assets/LingGuangV05/DesktopBridge/OSArt/Fonts/OpenSans-Bold.ttf`. Its Git blob and retained OpenSans Bold/Light/Regular/Semibold/Thin TMP atlases match their old DreamOS counterparts. FontTools name-table extraction from the actual TTF yields:

- `Digitized data copyright © 2010-2011, Google Corporation.`
- `Open Sans is a trademark of Google and may be registered in certain jurisdictions.`
- `Licensed under the Apache License, Version 2.0`
- `http://www.apache.org/licenses/LICENSE-2.0`

Accordingly, keep the old packaging Apache attribution. The conflicting OFL statements in OSArt README and `Documentation/LingGuang-Incremental/开发引用.md` should not override binary-specific evidence.

## Removed pack and Lucide evidence

`Assets/LingGuangV05/DesktopBridge/OSArt/README.md`, `Tools/OSArt/Sprites/README.md`, `Tools/OSArt/Audio/README.md`, `Tools/OSArt/Audio/make_audio.py`, `Tools/OSArt/Glyphs/icons.py`, `Tools/CaseMenuAudio`, and `Documentation/LingGuang-Incremental/开发引用.md` document replacement graphics, generated audio and rewritten implementation. The legacy-named `Music_JackTheLumberer.wav` now contains synthesized “Pocket Lantern”; the mixer retains structural names/GUIDs rather than original audio content.

A current-versus-`c4e02003` non-meta Git blob comparison found 35 exact old DreamOS assets, principally layout/library/localization structural data plus the Open Sans font/atlases, and one old Feel asset (Roboto Mono). Therefore do not state that absolutely no old structural assets remain, or that all migrated material has no licensing obligations.

The archived Lucide manifest has 328 entries. Of 309 corresponding current OSArt files, zero matched its Lucide `after_sha256` values and zero matched its original `before_sha256` values. Current hand-drawn glyph generator and OSArt/Aero README evidence supports removing the missing runtime Lucide required path. `Documentation/DreamOS-Lucide/LICENSE.txt` and backup material are archival source records outside Assets, not evidence of current runtime Lucide content.

## Keep existing validations and notices

- **UI fonts:** `Assets/StreamingAssets/Licenses/Fonts/{FONT_CREDITS.txt,SOURCE_MANIFEST.json,MiSans-license.pdf,Inter-LICENSE.txt,SourceHanSerif-LICENSE.txt}`. Preserve checks of MiSans Regular/Medium/Semibold weights and the exact manifest. Do not distribute standalone MiSans font files.
- **Other retained fonts:** `Assets/LingGuangV05/Localization/Fonts/OriginSerif-LICENSE-OFL.txt`, `Assets/Emergence/Resources/Fonts/OFL.txt`, and `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`. Retain Noto Sans CJK and Rajdhani attribution: their Resources atlases remain and can ship even if old scenes are not selected. Keep OriginSerif/Source Han Bold and Liberation Sans notices.
- **Oxygen:** actively loaded by `AeroLicensedIconLoader` from `StreamingAssets/HongmengOS-Aero2010/Oxygen`. Preserve NOTICE, manifest, full license files, editable source files, and source-versus-built StreamingAssets checks. Existing NOTICE identifies LGPL-3.0-or-later and three modified icons: Reminder.png, Widget Library.png, Messaging.png. This obligation survives the desktop art rewrite.
- **Photos:** retain `PHOTO_CREDITS.txt`, `GROUP_CHAT_PHOTO_CREDITS.txt`, and `ENDING_TRIAL_PHOTO_CREDITS.txt` under `Assets/LingGuangV05/Resources/LingGuangV05/Media`.
- **Original group-chat illustrations:** retain `Design/素材源码/group-chat/draw_chat_scenes.py` and `manifest.json`, plus current asset hash checks. Ten manifest entries were checked; zero asset hash mismatches.
- **Blackjack:** retain both LICENSE.txt and NOTICE.md under `Assets/LingGuangV05/ThirdParty/Blackjack`.
- **Inference:** retain canonical model provenance/license, LLMUnity, LlamaLib/native and other actual runtime notices, model uniqueness and hash checks. Canonical filename remains `Qwen3.5-4B-Q4_K_M.gguf`. Do not infer model identity from the stale upstream licensing draft.

BlipTalk's current implementation documentation describes inspiration from an MIT project and zero copied audio; a reference acknowledgment is reasonable, but copied code was not established by this audit. Upstream draft statements about quantizer/embedding, Bliss, Suno, or voice material are not authoritative substitutes for the actual retained files and model provenance.

## Provenance and final packaging boundary

Do not weaken `RequireBuildSource`, exact model validation, source-versus-built StreamingAssets checks, archive volume limits, or archive integrity checks to work around obsolete paths. Change only the obsolete inventory entries and their corresponding prose.

Captured source roots are Assets, Packages, ProjectSettings, and Design. Documentation and Tools files are useful audit evidence but cannot simply be passed to `RequireBuildSource` as if they were captured. Any newly introduced shipping notice must either be inside a captured root before the build, or be explicitly tracked and verified as separate packaging metadata. The existing companion Roboto notice is packaging metadata; update and verify it deliberately.

After the final build, compare actual included dependencies and copied notices to this source inventory. This audit does not assert final Player license completeness, successful packaging, archive integrity, or complete gameplay acceptance.
