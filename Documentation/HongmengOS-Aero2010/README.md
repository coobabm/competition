# hongmengos 2010 · Aero Desktop

Scene: `Assets/DreamOS - Complete OS UI/Scenes/2010desktop.unity`

Independent copy of the current DreamOS Desktop scene. The modern `Desktop`, `90desktop`, shared UI Manager and old Retro resources are preserved. Aero-specific artwork, scripts, materials, app library and meshes are under `Assets/HongmengOS/Aero2010`.

## Design and reference

- Period reference: **Dell UltraSharp U2311H**, a 23-inch Full HD LCD from the 2010 era.
- Official reference: https://i.dell.com/images/global/products/monitors/U2311H.pdf
- The scene includes an original procedural mesh interpretation of the period industrial design, **not a downloaded/licensed Dell CAD model**. It uses hongmengos branding rather than claiming to be a Dell product.
- Flat 16:9 aperture, 1920×1080 source desktop; straight-on near-full-frame gameplay camera. Stand/rear casing are modeled but below/outside the default close-up.
- Licensed Oxygen application icons at runtime, original glossy fallback icons, blue Aurora wallpaper, Aero-style translucent blue window chrome and taskbar, and a two-column Start menu. These evoke the period; no Microsoft wallpapers or logos were downloaded.
- No CRT barrel distortion, raster scanlines, discharge flash or intentionally low-resolution filter.

## Controls

- Start orb opens the program menu; search filters available applications.
- Existing desktop icons/window/taskbar events are retained. Closed apps do not remain pinned in the taskbar.
- Right-edge physical power key switches the LCD backlight and desktop input off/on, with a short mechanical sound.
- Four OSD keys (top to bottom): standard preset, brightness down, brightness up, display OSD.
- Taskbar far-right strip shows desktop/restores minimized windows.

## Implementation

DreamOS Canvas → UI-only camera → 1920×1080 RenderTexture → world-space LCD panel. The 3D model is on layer 0; the source UI is on layer 5. Flat-panel input coordinates are relayed to the source uGUI raycasters, including click, double-click, drag, scroll and text focus. No system pointer position is changed.

Aero glass is lightweight translucent tint/gradient/specular-band rendering, not a Windows DWM compositor or true per-window background blur. DreamOS application functionality remains a game UI simulation; this is not a Windows emulator.

Source rendering costs one extra 1080p camera pass (RGBA32 color ~8 MiB plus depth and driver buffers). The input bridge caches raycasters; pointer raycasts scale with active source graphics. Generated resources are local and there are no remote/paid runtime calls.

## Validation

See `Evidence/HongmengOS-Aero2010/` for live Editor screenshots, protected-file hashes and the integration check report. Player build and hardware-platform validation are not included unless explicitly recorded there.

## Licensed icon replacement

After visual review, the 13 application icons are replaced at runtime with **unmodified KDE Oxygen** icons (glossy late-2000s design, not Microsoft Windows icon extracts). Desktop icons, window title icons, taskbar icons, Start menu items and the system-page emblem share the same set.

Upstream: https://github.com/KDE/oxygen-icons
Pinned revision: `65ccf0752df19c9cf97da4884e41bfd4e8c8ec69`
License: **LGPL-3.0-or-later**, with the upstream Oxygen artwork clarification.

The separate files under `Assets/StreamingAssets/HongmengOS-Aero2010/Oxygen` are loaded by `AeroLicensedIconLoader`; they are deliberately not baked into a closed sprite atlas. Keep the entire Oxygen folder (including NOTICE, licenses, source, and manifest) in distributed builds. It includes the original preferred editable SVG/SVGZ sources, authors, full LGPL/GPL texts, original paths, and SHA-256 hashes. Permit replacement/modification of this artwork library and do not impose an EULA restriction that conflicts with its license. Verify the packaged StreamingAssets folder and license notice when producing the release build.

The first original icons remain as startup/error fallbacks and as Edit-mode placeholders. Runtime libraries/textures are cloned; source asset files and the other scenes are untouched. Desktop loading is local; Android/WebGL loading uses platform StreamingAssets URLs. No third-party network fetch is performed in the desktop player.

## Light-window text contrast

The 2010 scene uses dark text on light application content, with opaque dark-gray secondary labels. Deep-blue shell areas, dark terminal content, image overlays and avatar badges keep appropriate light text. Dynamic DreamOS list/page templates require scene-local copies; changing only the already-instantiated window does not fix the next generated mail, chat, playlist or note row.

The contrast installer is Editor-only and restricted to `2010desktop`. Its template copies and browser-library copies stay inside the Aero2010 theme folder. Do not redirect other scenes to these copies or edit the shared DreamOS UI Manager to obtain the same result.

Release checks still required for the target platform: inspect the packaged Oxygen folder, preserve NOTICE/licenses/source/manifest, and check the target game's distribution terms for conflicting restrictions on the artwork library. Editor validation is not a packaged Player test.

### Recorded Editor result

The completed Play-mode regression passed **45/45** checks. All **13** application first screens were inspected/captured; generated app UI contained **0 shared-theme references** and that run captured **0 Error/Exception/Assert** logs. Icon cloning/destruction fallback also passed. Original scene/shared-resource hashes remain unchanged. Temporary QA scripts have been moved out of Assets into Documentation/Validation, and a post-cleanup compile and missing-script scan passed. See `Evidence/HongmengOS-Aero2010/validation-summary.json` for the precise boundary.
