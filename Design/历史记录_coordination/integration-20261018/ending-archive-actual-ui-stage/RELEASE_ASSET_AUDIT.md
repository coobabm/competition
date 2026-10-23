# Read-only release asset audit

Inspected production with `git diff HEAD` only, on 2026-10-19. No production writes, restores, Unity or compilation. This records the diff at inspection time; recheck if the serial editor subsequently writes these assets again.

## Generated-only changes suitable for restoration

| Exact project-relative path | Entire observed content diff |
| --- | --- |
| `Assets/LingGuangV05/Localization/Fonts/Desktop CJK.asset` | Generated `m_FontFeatureTable` ligature and glyph-pair additions only. |
| `Assets/LingGuangV05/Resources/LingGuangV05/Fonts/OriginMono SDF.asset` | Generated feature table additions; fallback `[]` becomes a single null `{fileID: 0}` item. |
| `Assets/LingGuangV05/Resources/LingGuangV05/Fonts/OriginSerif SDF.asset` | Only fallback `[]` becomes one null `{fileID: 0}` item. No actual font reference added. |
| `Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/ReadingSerif.asset` | Atlas bytes, glyph/character tables, and used/free atlas rectangles only. |
| `Assets/LingGuangV05/Resources/LingGuangV05/Fonts/UiV4/UiSans.asset` | Atlas bytes, glyph/character tables, used/free rectangles and generated feature table only. |
| `Assets/LingGuangV05/CaseMenu/Art/Materials/Decor_Ivory Ceramic.mat` | Only `_Color.r: 0.81176466` becomes `0.8117647`; insignificant serialization precision change. |

The five font diffs contain no font-source, population mode, atlas dimension, padding, render mode or non-null fallback reference change. No intentional performance configuration is mixed into these six file diffs. Restoring their entire current diff preserves the HEAD configuration and removes the generated cache/serialization changes.

## No current content difference from HEAD

- `Assets/Emergence/VisualDemos/NeuronLife/SparkNeuronDemoFont.asset`
- `Assets/HolenderGames/UpgradesTreePro/Fonts/Hardpixel-nn51 SDF.asset`
- `Assets/HongmengOS/Aero2010/Rendering/LCD 1920x1080.renderTexture`
- `Assets/Settings/PC_RPAsset.asset`
- `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `Packages/Aura/.gitignore`

Some paths produce line-ending normalization warnings but no content patch. There is no observed change in this set to classify as an intentional performance adjustment or to restore.

## Package manifests: root owns portable replacement

`Packages/manifest.json` and `Packages/packages-lock.json` replace the three Git dependencies with local absolute `file:C:/Users/xomem/Documents/ChatGPT/games/.coordination/build-dependencies/...` locations. The lock also changes `source` from `git` to `local` and removes the matching Git hashes. No other dependency versions change in these diffs.

The affected dependencies and original resolutions are:

- `ai.undream.llm`: `https://github.com/undreamai/LLMUnity.git#v3.0.3`, hash `9931a9ea46906b1f43adaf9498c05ccf227b5d0b`.
- `com.coplaydev.unity-mcp`: `https://github.com/CoplayDev/unity-mcp.git?path=/MCPForUnity#30d22075093d1d35dfb0091c1c7550e9ad948577`, same hash.
- `com.ramenvr.aura-unity`: `https://github.com/ramenvr/AuraForUnityPlugin.git`, hash `558ea3c324c2cfd2803f3486bedbe9cbb740df9d`.

These are local dependency-resolution overrides, separate from performance assets. The parent owns conversion to portable manifests and the final build.
