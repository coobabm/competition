# EditMode UI / media audit — 2026-10-20

Scope: read-only inspection of `competition-integration-20261020`; no Unity process, source modification, build, commit, package or release. Nothing in `competition-latest-20261016` was touched. The title remains v5 preview pending Claude v6 delivery. Candidate files below are outside the project.

## Findings

| Failed test | Classification | Evidence and correction |
|---|---|---|
| `XgFeatureTests.FreshUiHidesFuturePagesUntilTheirActualMilestones` | Old contract-page assertion; fixture name also predates the modern opening | `XgSim.Features.cs:30` opens contracts at stage 3. `Design/六小时整合决策记录_20261019.md:65–72`, D017 final correction, explicitly allows seeing quality requirements without signing; `XgEarlyContractsTests.cs:25–36` tests this same contract. Candidate preserves stage gates, asserts low/no checkpoint still shows requirements, and asserts every contract remains unsignable without quality. Renames fixture to LegacyUi because it creates XgSim without BeginOpening. Modern flow remains protected by `XgTeachingFlowTests.TwelveAnswersOpenTechButOnlyBuyingTheCurveOpensTrain` using actual answer settlement and BuyNode. |
| `XgTechTreeTests.LaddersBecomeOneSquareInChainOrder` | Old kind-only definition of a display chain | `XgTechTree.cs:49–51` deliberately extends ChainNode beyond Depth/Width to board.logic; `XgSim.BoardLogic.cs:8–16` defines four Research steps and the separate nextmove purchase. `Design/第八轮_桌游训练数据_逻辑判断区_生活分支.md:14–23` explicitly says one node with four levels. Candidate requires precisely logic1–4, Research kind, parent chain order, same entry identity, and a separate nextmove square; other chains still must be Depth/Width. |
| `XgTechPlainTests.EveryTechNodeIsHandWritten` | Test still checks an unused legacy copy table | `rg XgTechPlain Assets -g '*.cs'` finds only the class and its tests. Current cards use `XgTechTreeCopy.Card`/Summary/Effect; tooltip uses `XgTechTreeCopy.Detail` at `XgTechTreePage.cs:1114`. `XgTechTreeCopy.cs:95` contains authored curve copy, and :103 exposes current `Covered` policy. Candidate tests Covered, real bilingual three-line output, nonempty detail, no review placeholder and era lexicon. It explicitly reveals label.auto so the mystery fallback cannot mask missing copy. Legacy line-format tests remain as compatibility tests. No fourth card line or detail button is added. |
| `DesktopMediaTests.EveryAuthoredNewsAndThreadHasAnIndependentPhotograph` | Real new media coverage gap; no test relaxation needed to add original illustrations | Full static scan of authored Era news/tieba keys vs Photos found exactly two absent keys: `era_lz2_t03_discussion` and `era_lz2_t04_discussion`. Their article metadata marks both `original-fictional-discussion`. The check enforces non-null sprite, separate Texture2D identity, and `Photos/` name prefix; it does not test photographic realism. Both can be filled by independent original knowledge illustrations with honest credits. Do not alias unrelated photos, copy the same texture, exempt all new discussions or delete the test. |

## Candidate patches

- `XgFeatureTests.cs.patch`
- `XgTechTreeTests.cs.patch`
- `XgTechPlainTests.cs.patch`
- `sources/`: reviewable complete candidate files, not production replacements to copy blindly.
- `source-sha256.json`: exact original test-file hashes.
- `build_candidates.py`: reproducible generation with unique exact-match guards.

Validation actually performed: all three patches passed one `git apply --check` against the integration working tree after normalizing patches to LF. Target test files had clean git status when checked. C# compile and NUnit execution were **not** performed; only the coordinator may apply and run them. The existing failed XML remains unchanged.

The modern three-line/12px-gray/icon-click/0.35s-hover contract is recorded in `Design/整合记录.md:756–781`; the later user instruction overrides older documentation that still says Details button. Current `XgTechTreePage.cs:189` uses `.35f`; `FillTooltip` does not call XgTechPlain. Candidate tests do not change production UI or equipment expansion.

## Media handoff

1. `Assets/LingGuangV05/Resources/LingGuangV05/Media/Photos/era_lz2_t03_discussion.png`
   - Topic: Does letting everyone teach a chatbot always make it better? Content discusses harmful crowd teaching and source quality; no need to depict a real Microsoft UI or person.
   - Existing article fact source: https://blogs.microsoft.com/blog/2016/03/25/learning-tays-introduction/ (stored in era_events.json:127).
   - Suggested original illustration: several differently colored speech cards enter a simple learner; a review/filter step separates helpful examples from adversarial or noisy inputs. Avoid implying the historical failure had one proven cause beyond the original article.
2. `Assets/LingGuangV05/Resources/LingGuangV05/Media/Photos/era_lz2_t04_discussion.png`
   - Topic: Is a deeper network always better? Article discusses residual networks and transporting information/training signals through layers.
   - Existing article fact source: https://arxiv.org/abs/1512.03385 (stored in era_events.json:128).
   - Suggested original illustration: stacked simple network blocks with a clearly drawn skip connection alongside them. Caption/credits identify it as an original conceptual diagram, not a reproduced paper figure.

`DesktopMedia.Picture` resolves exactly `Resources.Load<Texture2D>("LingGuangV05/Media/Photos/" + key)` and creates a Sprite named `Photos/<key>` (`DesktopMedia.cs:47–69`). PNG/JPG accepted; no fixed pixel size contract in code/tests. `TiebaView.TopicLine:439–450` uses a 92×72 cover-cropped thumbnail and the visible generic caption “配图”; recommended canvas 920×720 with essential diagram content away from edges. Existing text-only fallback keeps topics usable but does not constitute approved omission of artwork.

`PHOTO_CREDITS.txt:3–4` already distinguishes technical reference imagery from news-event photography and records original composite reference art. `Design/老周问答与科技资讯_实施与对白清单_20261019.md:11` prohibits copying unauthorized photos/news materials. The stored source URLs above establish topic facts; they do **not** grant rights to copy image assets. Add accurate original-art credits, creation method, and topic-reference URLs, retain separate files/textures, and let Unity generate/import their .meta normally. The test name “Photograph” is historical wording broader than the actual assertion; renaming it to “IndependentIllustration” is optional clarification only and is unnecessary to make valid independent artwork pass.

After integration: rerun these cases and the full EditMode suite through the coordinator; inspect both actual Tieba rows in-game at their date gates and at the 92×72 display size. New original diagrams need visual review for legibility and honest presentation before calling the media issue complete.
