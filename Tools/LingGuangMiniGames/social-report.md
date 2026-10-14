# Native social mini-games — implementation report

Date: 2026-10-12. Scope: 10 social games. No scene, package, wallet, parent integration, or existing story changes.

## Files
- `Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgSocialGames.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgSocialGames.PoetryAndReviews.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgSocialGames.Deduction.cs`
- `Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgSocialGames.Conversations.cs`
- `Assets/LingGuangV05/XingGuang/Core/XgMiniSocialRules.cs`
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgMiniSocialTests.cs`

## Factory
`XgSocialGames.Create(string id, XgMiniContext context)` returns the native implementation for all ten assigned IDs and null for other IDs. Root calls Build once, Tick while active, Dispose on switch.

## Mechanic mapping
| ID | Native interaction retained |
|---|---|
| esp | 8-second hidden emotion match; player label vs companion reveal; 12 turns, match and true-label counters are distinct. |
| captcha12306 | 8 selectable vector-shape cells, 8-second expiry, all-or-nothing exact target mask, later color+shape predicates, skill-dependent delayed hints, refresh preserves remaining time, 12 screens. |
| feihualing | 5 bouts, keyword, real poetry corpus, no repeats, 10-second player selection, alternating skill-dependent AI line/fabrication; four lines each yield a draw. |
| doutu | 3-second reply cards, opponent 12 HP/player 3 HP, combo critical, AI joins turn four, advice/assist accuracy follows skill. |
| zhaoshuijun | 3 pages of 12 timestamped reviews, 3/4/5 fake reviews, duplicate text/timestamp/cashback/chronology evidence; 45-second timer, wrong click costs 3 seconds; delayed AI suspicions. |
| shuishiwodi | Four seats, player may be undercover; similar word pairs, describe, mark semantic similarity, then vote; authored skill tiers from yes/no through tags/sentences to bluffing, NPC votes, ties, elimination and final reveal. |
| kaixincidian | 12-question increasing-difficulty ladder, 15-second clock, one AI/50:50/swap lifeline each, safepoints at 4 and 8, voluntary exit, end-only bounded reward report. |
| chuanhua | Original/English bridge/back-translated Chinese, editable chunks with three replacements, explicit submission, repaired/missed/damaged token accounting; session-local corrected-token memory affects future errors. |
| turing | Two explicitly offline authored chat roles, six unique chosen probes, skill-dependent model dialogue tiers and typing delays, final A/B vote only, all six rounds reviewable without settlement. |
| qingwen | Twelve contextual messages, six emotion labels, AI guess, distinct empathy-reply phase, a consequence meter, skill-dependent authored reply option, early exit if meter depleted. |

## Deliberate adaptations / limitations
- Browser emoji imagery is replaced with vector silhouettes, colored cards, and Chinese text. Captcha uses geometric categories rather than a complete photo/object asset corpus.
- Content banks are curated native subsets/variants, not every original HTML line. Interaction structures are not a single generic yes/no template.
- Currency and persistent samples are only reported through `C.Report`; original HTML hard-coded prizes, localStorage wallet and negative-money deductions are not copied. Root applies balance/security limits. Review mistake time penalties and local quiz safepoints remain.
- No rewards for merely opening/restarting, idle timeout, AI suggestions, autonomous AI responses, or quietly leaving reviews unclicked. Turing role vote and completed player answers are required.
- Qingwen is explicitly an authored scenario, not deterministic real-person emotion inference. Turing's two partners are both offline story roles, not claimed real humans.
- Translation memory is in the live game instance, retained across that game's Restart but not save/load or page disposal. Root saved counters remain authoritative.
- Undercover uses up to three voting rounds and authored descriptions, not unrestricted text generation.
- No real model/network/API calls.

## Lifecycle / safety
All timers and delayed AI actions are scalar state consumed only by `Tick`. No coroutine, Task, Invoke, or global event subscription. Every generated button captures a view-generation number; Clear increments it, immediately disables old children, then destroys them. Dispose invalidates callbacks. Each answer/report path has a phase/answered/finished guard. Old views cannot submit into a new round. `C.IsActive` additionally gates input, time and settlement.

## Tests and evidence
- Tests written first; initial `dotnet test ... --filter XgMiniSocialTests` failed due missing XgMiniSocialRules, establishing red state. Concurrent unfinished board types also existed in that build.
- After implementation: `dotnet test Tools/XingGuang/tests/Tests.csproj --no-restore --filter XgMiniSocialTests -v quiet` passed **11 / 11** (2026-10-12).
- Covers real/theme/repeated poetry acceptance, exact captcha masks including empty/out-of-bounds masks, all seven quiz safepoint boundaries, translation fixes/misses/damaged-good-token distinctions, and quiz bank uniqueness/answer integrity.
- Existing unrelated warning: XgSim.DayStats.cs same-variable comparison; did not edit it.
- Unity runtime/UI compilation, EditMode inside Unity, and visual/input acceptance belong to root's central verification and are **not claimed** here.

## Complexity
All UI board sizes are bounded (4 choices, 8 captcha cells, 12 reviews, 4 seats). Per-tick timer work is O(1), captcha hint sweep O(8), review hint sweep O(12). Poetry filtering is O(P) for P authored lines; quiz/probe selection O(B) for bank size. Memory is O(P+B+R) for bounded content/session history; no unbounded chat/log accumulation.

## Follow-up checks
- Settlement unit count in quiz/undercover is explicitly capped at 8 to match `SubmitMiniGame`'s server-side/native-domain batch bound. Twelve quiz questions still play; main-economy units are intentionally bounded.
- A later repeat of the same dotnet test command was temporarily blocked by concurrently added `XgMiniIntegrationTests.cs:55` referencing UnityEngine in the pure .NET project. Root was notified; the earlier 11/11 run remains valid evidence for the pure social rules but not a claim that the entire evolving project currently passes.
- Root repaired the conditional Unity serialization test; the social NUnit command was rerun successfully: **11 / 11**, 14 ms.
- Review fixes: ESP hides the pre-reveal prompt before rendering results; captcha cannot regenerate after submit; captcha cell labels use explicit dark ink and top-left placement so dark-host mode and vector silhouettes do not obscure them.

## 12306 visual-fidelity revision
- Replaced geometric silhouettes/color predicates with the existing `XgMiniBoardIcon` vector pictograms. No new assets, dependency or edits to shared icon/base-context code.
- The 21-object pool covers apples/watermelon/banana/grapes, fish/cat/rabbit/bee/butterfly, phone/computer/camera, and everyday objects.
- Targets alternate between category selection (fruit, animals, electronic devices, everyday objects) and specific-object selection every third screen. Every generated screen contains exactly 1–3 matching cells and 5–7 distractors; specific-object targets can have multiple pictograms of that object.
- Captions identify object names, not whether a cell matches. After submission, each cell identifies correct/missed/extra selection and the summary lists missed/extra counts plus the target.
- Retained 8-second timer, keyboard 1–8 and Enter, skill-dependent delayed hints, timer-preserving refresh, and prohibition of refreshing an already settled screen. Shared C.Text autosizing and icon CanvasRenderer requirements remain untouched.
- This supersedes the earlier geometric-category adaptation note. Root must refresh its screenshot/visual acceptance evidence for the revised object grid.
