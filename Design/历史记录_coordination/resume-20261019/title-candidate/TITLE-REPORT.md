# Main title decoration candidate

Scope: only the main-menu title **我的电脑活了**. All typography, title string, font face/weight/size, subtitles, controls and input remain unchanged. Production frozen; this candidate is outside the Unity project and has not been applied or visually accepted.

Measured from current `Assets/LingGuangV05/CaseMenu/CaseMenuHud.cs`: overlay reference1920×1080, title top-left(64,98), extent610×124, existing SansSemibold with42–66 auto-size and characterSpacing3. Title box ends at y222; subtitle box begins y234. The original static decoration fits their12-unit gap at top-left(70,222), width360, height12. It stays wholly outside both text rectangles. Coordinates are logical reference-canvas units, scaled by the existing menu scaler.

Treatment: a tiny open power ring feeds a restrained amber line; a short stepped signal and two small connected nodes suggest the computer learning; a solid terminal caret ends the line. Existing amber palette, no emoji/font glyph icons, downloaded imagery or third-party graphic assets. The geometry/code is newly authored specifically for this game. It adds no new font/image license dependency.

Implementation: one `CaseMenuTitleSignalGraphic` using the existing uGUI shared white material, fully procedural vertices, non-raycast. It is a child of existing Chrome, so existing fade/hide behavior applies. No Update/LateUpdate/coroutine, no infinite animation, no texture/material allocation, no additional canvas or input handler. Redraw only on normal uGUI mesh dirtying. It does not alter the settings/title action hierarchy.

Files: `CaseMenuHud.cs` candidate, `title-decoration.patch`, `prepare.py` reproducible generator. Parent may apply the checked patch after current visual QA. Final package acceptance still requires the real title screen at normal desktop size and a quick power/settings/title-action click check; this source candidate is not a screenshot or Player verification.

`git apply --check` passes. Geometry has136 vertices/68 triangles, wholly within the360×12 decoration rectangle. Original source SHA256 is recorded in `BASELINE-SHA256.txt`.

Parent review caught the need to explicitly require CanvasRenderer in this project's uGUI setup. Candidate generator includes `[RequireComponent(typeof(CanvasRenderer))]`; integration must retain it, and the preview checks the renderer's actual mesh.

## Lightweight actual-HUD preview

`TitleMarkPreview.cs` is a separate temporary Editor helper for the parent to place in `Assets/LocalBuild/Editor`. Run `-executeMethod TitleMarkPreview.CaptureAndExit`, optionally `-titleMarkOutput <absolute-directory>`. Default output is `LocalVerification/TitleMark/<timestamp>`. It renders1920×1080 and1280×720 PNGs plus `report.txt` on a neutral dark background, explicitly not a3D-menu screenshot.

It instantiates the real CaseMenuHud in an isolated preview scene, injects only the temporary canvas/font fields, and invokes the actual `BuildChrome` and `BuildTitleActions`. It deliberately does **not** call full `Build`/`BuildSettings`: the latter reads GamePerformance preferences. No runtime/archive callbacks are bound or invoked. It does not access PlayerPrefs, saves, font builders or AssetDatabase.SaveAssets. Checks cover exact title string, original face/style/size/tracking/rectangle, no text overflow, non-raycast decoration and the actual CanvasRenderer mesh. Existing font assets may warm glyphs in memory during rendering; no assets are saved. The parent still needs to run this helper; it has not been executed by this agent.
