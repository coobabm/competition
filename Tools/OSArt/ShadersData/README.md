# ShadersData – blur shaders, materials, render textures, localization

Everything under `Assets/LingGuangV05/DesktopBridge/OSArt/` in these categories is written by us.

| Output | Source |
|---|---|
| `Shaders/UIBlur-Fast.shader`, `UIBlur-Masked.shader` | hand-written ShaderLab/CG (no generator) |
| `Materials/Blur_*.mat` (24), `_Demo Assets/Models/Ground.mat`, `Textures/Placeholders/Game Hub/3D Test Renderer.renderTexture`, `Textures/Rendering/Video Player.renderTexture` | `python3 genmats.py` |
| `Localization/Tables/*.asset` (7), `Localization/Languages/*.asset` (en/de/tr), `Localization/Localization Settings.asset` | `python3 genloc.py` + `locdata_*.py` |

Run either script from any directory; paths resolve from the script's location. `.meta` files are never written.

## Shaders
- Property names/types and shader names (`DreamOS/UI Blur Fast`, `DreamOS/UI Blur Masked`) are fixed: `_Size` is animated by `UIBlur`; the
  grab names `_GrabTexture` (Fast) and `_HBlur`/`_VBlur` (Masked) are kept.
- Built-in pipeline: 2 grab passes, each a 9-tap gaussian done with 5 bilinear fetches (10 reads per pixel; the old shaders used 27 + distortion).
  Fast keeps optional glass refraction (`_BumpAmt`, skipped when 0) and the `_Color`/`_MainTex` tint. Masked scales the radius by the sprite alpha.
- Stencil and colour-mask properties are now wired into every pass (they were declared but unused before), so UI `Mask` works.
- The project runs URP, which cannot GrabPass. Both shaders have a first SubShader tagged `UniversalPipeline` that draws a translucent
  frosted veil whose strength follows `_Size`; the built-in SubShader is used on the built-in pipeline. `UIManagerBlur` still switches the blur objects off under SRP.

## Materials
Same shader GUIDs and `m_Name`. Initial `_Size` per material is kept (UIBlur resets it to 0 on Awake). Stencil/mask floats were corrupt (255) in the
originals; they are now the standard UI defaults (comp 8, ref 0, op 0, masks 255, colour mask 15). `_BumpAmt` is 0 and `_Color` a faint cool white.
Render textures are 1920x1080, depth-stencil 92, same colour format / sRGB flag as before.

## Localization
Tables hold keys only; values live in the language assets. Keys, table IDs, language IDs and all GUIDs are unchanged (`genloc.py` asserts that the
keys match the files on disk). English is our own wording, German and Turkish are our own translations of it; brand names (DreamOS, Cori, Discord,
Twitter, YouTube) are gone from the values. The literal triggers `What's up?`, `SEQ_0` and the `CreateStoryTeller(...)` call stay verbatim because
the sample conversations depend on them. The Simplified Chinese language is not stored here: `DesktopLocalizationInstaller` copies the English asset
and fills it from `Resources/Localization/NativeChinese.tsv` (keyed by table index + key), so key order must not change.
Turkish `localizedName` is now `Türkçe` (display name only).
