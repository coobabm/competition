# Title v3 staged candidate — 2026-10-20

Status: candidate only; no formal project writes, no Unity launch, no commit.

## Sources reviewed
- 发布材料/封面与宣传视频_20261019/TapTap物料_v3_20261020/游戏Logo_含图标_透明底_1814x1239.png
- Same directory: logo_icon.html and 上传说明.md.
- Current CaseMenuHud.BuildChrome and existing CaseMenuHudTests.

The transparent PNG already contains the complete approved title: two-line lettering, monitor 活 icon, spider and coloured threads, glow, power-ring/signal decoration. Use the PNG exactly; HTML is provenance/reference only and is not a runtime dependency.

## Minimal integration
1. Apply title-v3.patch from the project root.
2. Apply title-v3-tests.patch; the previous test expected the main title to be TMP text and must now verify the artwork instead. Settings-title font test is preserved.
3. Apply title-v3-preview.patch to update the existing TitleMarkPreview QA helper. Its 1920x1080 and 1280x720 captures, disclosure checks and non-interception checks remain; it now checks resource identity, pixel dimensions, no tint/crop, aspect ratio, English eyebrow and no subtitle overlap.
4. Copy the staged Assets/LingGuangV05/Resources/LingGuangV05/CaseMenu folder and adjacent CaseMenu.meta to the matching project path.
5. Import once, run the HUD tests and capture menu screenshots at the supported small/large resolutions, then perform the existing boot/shutdown checks.

Only CaseMenuHud.BuildChrome changes in production code. The PNG is a Resources-loaded Texture2D displayed by RawImage under the existing Chrome CanvasGroup. Width 410 reference pixels; height calculated from original aspect ratio (about 280.04). Top-left remains (64,98) on the existing 1920x1080 reference canvas. The subheading moves to eight pixels below the artwork. Image tint is white, full UV is retained, raycastTarget is false, and existing Chrome fade applies. No new animation or input logic.

The existing English eyebrow MY COMPUTER IS ALIVE / 2016 remains unchanged in both locales. A missing-asset fallback uses AppNames.Game(current language) with the original TMP layout. The original TitleSignal object is no longer instantiated, preventing the duplicated power/signal motif; its unused class remains to keep this patch narrow. Scene, BootScreen, controller, buttons, other fonts and font sizes are unchanged.

## Resource manifest
- Assets/LingGuangV05/Resources/LingGuangV05/CaseMenu.meta: folder GUID a5c93e975e0446d7b6ebdbe4bad1f37b
- Assets/LingGuangV05/Resources/LingGuangV05/CaseMenu/TitleLogoV3.png: byte-identical source PNG, 1814x1239
- Assets/LingGuangV05/Resources/LingGuangV05/CaseMenu/TitleLogoV3.png.meta: GUID c62fae54a45e421a8478dc874ea994d9; default Texture2D, alpha retained, sRGB, bilinear, clamp, no mipmaps, NPOT unchanged, max 2048, uncompressed. Runtime key: LingGuangV05/CaseMenu/TitleLogoV3.
- PNG SHA256: F85940B23AAD99A73747F2018BA5A11211595591542B1B89DB6F46127874066C

## Validation
- All three staged C# files: Roslyn syntax errors 0.
- All three patches: git apply --check succeeds against the current project.
- Source/staged PNG SHA256 identical.
- No Unity rendering or interaction claim yet. The wider/taller two-line artwork must be inspected over the actual room at both resolutions before packaging.