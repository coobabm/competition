# Typography implementation/layout audit — 2026-10-19

Read-only production audit of `competition-latest-20261016`. No assets/C# changed, no Unity started. Font licensing/source inventory is owned by the separate license audit; family names below identify existing code/assets and are **not** a new commercial-use determination.

## Recommendation

Give .exe a deliberate, compact hierarchy: a quiet CJK sans body, real medium navigation/controls, real semibold headings, and a separate stable numerical face. Reserve the reading serif for long-form book/narrative content. The current foundation already supports these roles, so the smallest coherent change is explicit role styling at .exe construction sites plus a handful of spacing corrections. Avoid global `TMP_Settings.defaultFontAsset` replacement or scene-wide text mutation: native 2016 app chrome and light Bodu Crowd share infrastructure.

All proposed sizes below are **logical TMP sizes at the .exe's 1236 × 693 design surface**, not guaranteed physical pixels. `XingGuangView.cs:118` attaches `UiFitScale`; `DesktopBridge/UiFitScale.cs:38–60` scales the entire view with the window. Check normal/maximized at the actual desktop scale before interpreting “14” as 14 screen pixels.

## Existing implementation map

Paths in this section are relative to `Assets/LingGuangV05/`.

| Entry | Current behavior / implication |
|---|---|
| `XingGuang/Desktop/XingGuangView.cs:116` | Constructs dark `XgUi(XgUiV4Fonts.Sans ?? fontAsset, …, true)`. This is the main .exe font boundary. |
| `XingGuang/Desktop/XgUi.cs:202–210` | `Text` sets supplied font/size/color, normal wrapping, **Overflow**, rich text; no role, weight, spacing or auto-size policy. `Button` uses it and switches to NoWrap (:244–246). Styling every label here indiscriminately would also change light apps. |
| `XingGuang/Desktop/XgUiV4Fonts.cs:8–17` | Lazy Resources selectors `Sans`, `SansMedium`, `SansSemibold`, `Reading`, `Numbers`; useful stable abstraction if the licensed font family is replaced later. |
| `Editor/UiV4FontWeights.cs:43–54` + current UiSans asset | Already maps real 500→Medium, 600/700→Semibold, with reciprocal tables on the heavier faces. Existing `<b>`/Bold on UiSans is **not simply all faux bold**. Current assets confirm the references. |
| `XingGuang/Desktop/XingGuangView.Frame.cs:61,103` | Numerical font applied to top-bar values and main progress values; `fontFeatures.Clear()` disables features. `FixedDigits` supplies `<mspace=0.65em>` number runs. Other page metrics still generally use UiSans. |
| `XingGuang/Desktop/XgReadingWindow.cs:108–109` | Book body 19, separate `Reading` face, `richText=false`, scroll layout. Keep this semantic separation. |
| `DesktopBridge/Zhongbao/ZhongbaoView.cs:397–398` | Light `new XgUi(font,window)` uses native/controller CJK font. `XgLabelPage` serves both dark .exe teaching and this light host via `DarkTeaching`; all teaching changes must retain that distinction. |
| `DesktopBridge/NativeAppFontFallback.cs` | Preserves the native primary and material using a shared clone per original font, appends dynamic Noto CJK fallback only for missing glyphs. This is a coverage boundary, not a restyling hook. |
| `XingGuangView.cs:155–157` | `font` field, SetupOverlay's explicit font argument and Juice still receive original `fontAsset`, while `ui.font` is UiSans. Audit these deliberate/legacy split paths when seeking whole-.exe consistency; do not assume the constructor change covers every .exe text. Boot/menu also explicitly select the semibold face. |

`XgDark` lives inside `XgUi.cs:42`, not in a separate file. Ink #F2F4F7, Muted #97999E and Dim #53555A are already distinct. Use Muted for readable helper text; Dim should remain decorative/disabled. Changing the font alone will not make essential small Dim labels readable.

## Minimal role contract

Prefer one small **dark-only, opt-in** `ApplyTextRole` helper/overload with an explicit role enum; retain the current `Text` signature for compatibility. Configure once when building a label, not during every Refresh. Font selection/weight should be explicit, with `FontStyles.Normal` when assigning a whole-label Medium/Semibold face. Avoid guessing a role from GameObject name, text content, current size or locale.

| Role | Proposed size / face | Spacing and overflow |
|---|---|---|
| Page/modal title | 20–22 Semibold | characterSpacing 0; lineSpacing 0; keep actual wrapping/scroll policy |
| Section heading | 16 Medium | characterSpacing 0; lineSpacing 0 |
| Main sidebar | 15 Medium | NoWrap + Ellipsis, same reserved rectangle and tooltip; selected state changes color, not width/weight |
| Normal controls/body | 15 Regular body / Medium controls | characterSpacing 0; body lineSpacing 4 as initial TMP setting; buttons lineSpacing 0 |
| Secondary/dense row | 13 Regular; 14 for important dense names | lineSpacing 2–4; Muted, not Dim; 12 only for truly secondary fixed micro-labels |
| Data value | 14 compact / 24 hero, numerical Regular | fixed advance for digits only, right-aligned columns, zero added characterSpacing; keep units outside fixed run |
| Book/long-form reading | 19 existing reading face | start lineSpacing 6, paragraphSpacing 8; scroll rather than shrink |

TMP `lineSpacing`/`characterSpacing` properties are TMP adjustments, not CSS `line-height` or screen-pixel distances. The values are reproducible starting settings; measure resulting line height/fit after the chosen font metrics are loaded. No fake bold, outline or tracking should be used to compensate for an unsuitable face.

## Precise first-pass changes

1. **Sidebar and app frame** — `XingGuangView.Frame.cs:333–334`: row labels 14→15 Medium, keep the 20-high label slot initially; the 112-wide rail leaves approximately 94 width after margins, so English “Technology” and a long AI name must retain ellipsis/tooltip. `BuildNav` group labels at :281–282 have characterSpacing=12; reduce to 0 for Chinese/at most 1 for short English group captions **if visible** (the active four-item nav may hide grouping). Do not spread tracking over main labels. Footer YY link is forced to 11 at :299, resident controls to 11 at :313: use 12 Medium, with existing 24/26-high controls; keep their words short. Top-bar values stay 14, captions 12→13 where the existing half-bar permits; avoid increasing all top-bar columns simultaneously.
2. **Teach** — `XgLabelPage.cs:194–220`: question paper text currently autosizes 15–30, poem 28–52, question prompt 24. Keep task-specific large glyphs; do not replace all teaching stimuli with decorative serif/monospace. For dark ordinary prose start at 24 Regular with minimum 18; prefer readable overflow handling/short text layouts to tiny 15 fallback. Keep 24 answer buttons, use Medium rather than heavier simulated styles. Supporting suggestion/steps should be 15/13 Regular (current sizes already close). `XgLabelPage.Hall.cs:122–145` ordinary row text 14, lineSpacing 6: for DarkTeaching use 15 Regular, lineSpacing 4 only after confirming row two-line fit. Apply no change to the light Zhongbao host.
3. **Train** — `XgTrainPage.cs:86,107,124,152`: summary, metrics, hints and risk can shrink to 10. Raise relevant minimums to **12**, but do not simply clamp and leave Overflow: metrics should be split into stable label/value segments or abbreviate secondary labels with full tooltip. Existing 24-high metrics row can support 14–15 single-line text; keep important warning text 13 and allow its existing 28-high area to wrap if needed. Legend :121 is 11 Dim: 12 Muted. Chart ticks :118–120 remain 12, use numeric role. Primary train action :131–137 is 28 Bold, autosize15–28: keep 26 Semibold, min18; leave bill/subtext 14/13 separately. The bill reserves 104 width—do not allow the action string to grow into it. These are narrow typography corrections, not a redesign of the training layout.
4. **Tech tree** — `XgTechTreePage.cs:419–424`: node name currently 14 Normal, NoWrap/Ellipsis, autosize off despite a comment saying “two lines”; keep 14 Medium and explicit single-line policy initially. Do not rely on that stale comment to enable wrapping without checking links/price/detail button. Price 14 Bold→14 numeric Regular with a stable right/center slot. Tooltip title :548–549 19 Bold→20 Semibold. **Remove the negative `tipBody.lineSpacing=-6` at :568**, use 15 Regular/+4; the existing scroll/content measurement is suited to extra height. Icon glyph autosize down to 8/9 is icon fallback, not body copy: replace glyphs with existing sprite icons where possible, never compensate by making all node text that small.
5. **Profile/archive** — `XgHomePage.cs:62–63`: 14 speech body with lineSpacing12 is conspicuously different from other body text; use15 Regular/+4 in existing 120-high card and retain overflow indication/YY navigation. BigCard metric :134 stays24 numerical; note :137–138 12/+8→13/+4. Feed 13 is acceptable in fixed26-high rows; use Regular+Muted. `XingGuangView.Profile.cs` profile controls14→15 Medium (check the220-wide archive entry in English). `XingGuangView.PersonaArchive.cs`: heading21 Semibold, history15/+4, axis label13 and legend12 retained; keep the existing measured scroll content and rich-text escaping. Do not globally enlarge history until bilingual 14-day/64-reason content has been checked.

## Font coverage, weights and numerical grid risks

- Current chain is **Numeric/Reading → UiSans → Desktop CJK**; Sans/Medium/Semibold directly fall back to Desktop CJK. This is a workable coverage chain. Confirm no cycle after any font replacement; preserve source font inclusion for dynamic glyph generation in Player.
- UiSans has real medium/semibold mappings, but Numeric and Reading have **empty weight tables** and retained TMP boldStyle/boldSpacing settings. `<b>` or `FontStyles.Bold` on those primary faces may synthesize weight; keep their roles Regular unless a real compatible weight is built. Italic mappings are also empty; do not use synthetic italic for Chinese body text.
- A fallback glyph can change stroke weight, baseline and apparent size within a line. Probe CJK punctuation, full-width brackets, ¥/%/‰, minus U+2212, ×/÷/≤/≥/√, arrows, technical Greek, chess/brain symbols, uncommon user-name ideographs and both localized strings. Dynamic atlases reduce but do not guarantee all-glyph support; names and model output exceed a fixed UI corpus. Do not advertise “no tofu” based on a few seed characters.
- `XgUiV4Fonts.FixedDigits`: strings containing any `<` or `>` are returned untouched. Color-tagged metrics therefore lose its fixed-advance formatting. It also applies the same 0.65em advance to punctuation/signs (`.,:+-−/`); this preserves columns but can look loose. Prefer separate fixed-width numeric value labels; avoid extending this function over arbitrary rich text or CJK prose. Fixed digits do not prevent width jumps at99→100, unit changes, or negative signs—reserve a column width and right-align.
- Existing `fontFeatures.Clear()` also means a future font's tabular-number feature cannot be assumed to be active. Choose either measured `<mspace>` runs or supported tabular metrics deliberately; keep normal kerning for prose. The correct em width depends on the licensed replacement face and must be measured, not blindly inherited as0.65.
- Existing `_Sharpness=.5` on UiSans weights came from a prior screenshot study. It is specific evidence for current assets, not a transferable “beauty” setting for a new face. Avoid per-label material clones or stronger dilation; verify SDF sharpness at normal scale and scaled sizes.

## Canvas/layout performance boundaries

- All five UiV4 assets are Dynamic, multi-atlas enabled, clear-on-build disabled. New glyphs can allocate/upload atlas pages; real extra weights multiply warmed CJK coverage. Warm the shipped bilingual corpus once before display/build; do not call `TryAddCharacters` or enumerate every label each frame. No font build should run during the current visual QA freeze.
- Changing a label's font/weight/size triggers geometry/layout work. Cache role assignments at Build and refresh only changed text; preserve `XgBtn.Set`'s text equality check. A global font swap in Update would create avoidable full-canvas dirties.
- Tech details measure preferred title/cost size then `ForceRebuildLayoutImmediate(tipContent)` (`XgTechTreePage.cs:1007–1009`). Keep this at actual open/content/size changes. Removing negative line spacing should flow through this existing path, not add per-frame rebuilds.
- Archive already recalculates preferred height only when `layoutDirty` or viewport size changes (`XingGuangView.PersonaArchive.cs:90–97`); keep that pattern. Reading body uses VerticalLayoutGroup + ContentSizeFitter; do not add another competing manual resize loop.
- Many buttons are NoWrap but default Overflow from XgUi; autosizing masks width problems until English/new font metrics arrive. For each edited control choose one policy explicitly: adequate width, short label+tooltip, deliberate ellipsis, or known wrapping. Do not make autosize-to-9 the new house style.

## Bounded implementation and acceptance sequence

After the current QA freeze: (1) resolve commercially usable family/weights from the separate license audit; (2) preserve resource selectors and fallback identity, add dark-only role helper; (3) apply the five targeted surfaces above, first eliminating negative line spacing and tiny body auto-size; (4) inspect actual bilingual Teach/Train/Tech/Profile screens at normal and maximized scale with longest names, metric extremes and expanded details; (5) check missing-glyph logs, true weight mapping, material/atlas count and live-scroll/layout cost in the packaged Player. Do not claim aesthetic acceptance or runtime performance from this source-only audit.
