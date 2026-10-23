# Board menu glyph candidate

Staging only; no production edits, Unity, Player, .NET, model, build, Git staging or commit. Plain `git apply --check` passed against current production. Patch SHA256: `932d4698750f72e8777ecd627f5f73e5ddb99c3ec16f165a4a8a4753aacc4057`.

Actual parent-run log `../board-story-isolation-final-20261019.log` reports U+2708 and U+2726 missing from LingGuangV05_Desktop_Cjk, substituted by U+25A1 on Glyph objects (three warnings each). Source identifies LudoPanel.Glyph and ChineseCheckersPanel.Glyph. The existing hub resource directory contains only home/gomoku/go/chess images, so both newer games always fall back to their unsupported symbols.

The two-file candidate extends XgIcons' existing cached 64x64 original line-icon rasterizer with a five-stroke airplane and a six-point Chinese-checkers star. Existing nav icons retain their exact geometry, colors, rendering and cache behavior. Other hub ids still use their resource images or original fallback. These are original geometry using existing runtime code; no assets or third-party/font files are added or edited.

GameHubView now tries the icon first on both sidebar buttons and home cards, and creates the fallback TMP label only when no image exists. This avoids feeding unused unsupported glyphs to TMP before hiding them. Images keep the existing 40/58 size, position, preserve-aspect behavior and raycastTarget=false. Labels, names, button callbacks, gameplay and localization remain untouched. No new source paths are required for the previous selective staging list.

Static validation establishes source routing and clean patch application only. The parent must compile, reopen the Game Hub, inspect both menu locations for recognizable plane/star icons and check that U+2708/U+2726 warnings no longer recur. No runtime or screenshot pass is claimed here.
