# OSArt — desktop art and data (our own)

Everything here replaced the art that came with the DreamOS UI kit (2026-10-19). Files keep their original GUIDs, so
scenes and prefabs did not need re-authoring.

| Folder | Source |
|---|---|
| Textures (borders, shadows, spinner, system icons, placeholders) | drawn by code: `Tools/OSArt/Sprites`, `Tools/OSArt/Placeholders` |
| Textures/Icons (most 32x–256x glyphs) | Lucide icons (ISC, see `Textures/Icons/LUCIDE-LICENSE.txt`), rendered 2026-10-02 |
| Audio | synthesized: `Tools/OSArt/Audio` (mixer: three volume groups, Master/Music/Video) |
| Animations | regenerated with our own curves: `Tools/OSArt/Animation` |
| Shaders, Materials | hand-written blur shaders; materials from `Tools/OSArt/ShadersData` |
| Localization | our own wording: `Tools/OSArt/ShadersData/genloc.py` |
| Fonts | Open Sans (Google, SIL Open Font License 1.1) and TextMeshPro font assets generated from it |
| Prefabs, Resources | layout and library data built from our own scripts (`DesktopBridge/OS`, `DesktopBridge/Apps`) and the art above |
