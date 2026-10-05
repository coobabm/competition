# 888.vip 赌场支线 Implementation Plan

> Execution: inline, test-first; scoped final review. No commits/pushes or scene serialization edits.

**Goal:** 贴吧引流 → 原生浏览器输入 888.vip → 四种虚构赌博游戏 → 共用现有钱包；中期小关闭按钮常驻广告。
**Architecture:** XgSim partial owns serializable independent casino state/rules and uses IXgHost. Runtime-only WebBrowserLibrary clone registers a uGUI local page; no requests to the actual domain. A separate desktop host owns persistent-ad scheduling and page template. Reuse PrologueDesk, UiFitScale, XingGuangHost, forum schema and existing save pipeline.
**Tech Stack:** Existing Unity 6000.4.5f1, C#, uGUI/TMP, NUnit/.NET; no new packages/assets/models.
**Spec:** User request in this chat, 2026-10-04.

## Global constraints
- Work in /Users/kk/competition, Main.unity. Preserve other sessions' uncommitted changes.
- All wagers are fictional existing-wallet ¥, not real money; no payment, debt or cash-out.
- Four playable games: 3-reel slots; 3-dice big/small (triples lose); European 0–36 red/black roulette (0 loses); single-card dragon/tiger (tie loses).
- Stakes 10/50/100/500. Slots triple returns 20x, exactly a pair returns 1x; other games win returns 2x. Gross return includes stake. Rules visible before betting.
- Resolve wallet once before animation; no reward on page close/refresh; shared 1.5-second round lock across tabs, persisted RNG separate from training.
- Forum advert from stage 2 / July; popup starts stage 3 after 120 active eligible seconds, recurs 300 seconds after actual close, no timeout or stacking. Tiny but usable X (18 logical pixels), missed clicks do not dismiss. Hide during prologue/cutscenes/ending. Preserve normal tray notifications.
- Chinese and English. Local aliases for bare/www/http/https address; optionally route Bodu search to the native browser.

## Review focus
1. Insufficient/NaN/infinite wallet or invalid inputs: no mutation or RNG advancement.
2. Duplicate click, multi-tab, close during spin: exactly one settlement, no lost payout.
3. Reload/new save: independent state, bounded history, no training RNG disturbance.
4. Minimized browser, resized page, language switch: correct balances and button layout.
5. Ad misses, navigation and story overlays: persistent until X, not swallowing whole desktop.

## Task 1 — Rules and persistence
Files: new XingGuang/Core/XgSim.Casino.cs and XingGuang/Tests/Editor/XgCasinoTests.cs.
- [x] RED: test invalid bets/choice, 4 paytables, aggregate outcomes, shared cooldown, reload, RNG independence, bounded history, ad stages/close.
- [x] GREEN: CasinoRules.ReturnMultiplier; XgSim.TryCasinoBet(...); XgState.casino; AdvanceCasinoAd / CloseCasinoAd.
- [x] Run `dotnet test Tools/XingGuang/tests/Tests.csproj --filter XgCasinoTests`.

## Task 2 — Discovery and local website
Files: new DesktopBridge/Casino/{CasinoDesktop,CasinoPage}.cs; small hooks StoryDesktopPresenter.cs and BoduView.cs; forum.json and ForumTests.cs.
- [x] Add failing forum contract; implement bilingual forum thread.
- [x] Register cloned local browser library, four real interactive panels, rules/balance/result/history, visual deal/spin feedback, shared double-click guard.
- [x] No external gambling assets/code imported. Research: johakr/html5-slot-machine MIT, viewed README/LICENSE/CSS via gh; reference only because this project uses native uGUI rather than DOM.

## Task 3 — Popup and validation
- [x] Implement separate desktop ad with small X, persistent miss/body behavior, browser action and story suppression.
- [ ] Run .NET regression suites, Unity import/compile and relevant EditMode tests.
- [ ] Play with disk saves disabled: native address entry, four bets, insufficient funds, close/reopen, popup miss/X/cooldown, screenshots at normal and maximized sizes.
- [ ] Final source review; record results/limits in Evidence/Casino. Restore original editor state/save switch, verify Main scene unchanged.

## Execution ledger
- Baselines for touched existing files and Main/save hashes: /tmp/lingguang-casino-baseline.
- Rules RED: missing Casino APIs (red.log); GREEN: 21/21; actual JSON roundtrip added, 22/22 (json-test.log).
- Forum RED: missing casino_888; GREEN: Story 69/69. First full Xg suite 492/492.
- First Unity focused suite: 32/32 (job aa4a4e97dfb84b1b9097a9dc9851de30), before JSON test addition.
- Review findings: minimized browser focus; advert overlap with timed tray warnings; stale cross-tab result faces. Fixed without touching native vendor scripts.
- Unity Play initially blocked: existing Taohuo compile errors already corrected on disk by concurrent session but assemblies reload-locked. Did not touch that session's code or blindly unlock its lock. Main runtime disk-save flag restored true after failed attempt.
- External lock released by its owner; no forced UnlockReloadAssemblies call was made. Retrying fresh compilation/Play.
- Re-review: original 3 issues resolved. Follow-up same-frame-control lock fixed by synchronous Refresh after accepted bet. QA completion now distinguishes interrupted runs and minimized-browser check verifies actual pointer interaction.
- Full .NET after concurrent integrations: Xg 521, Story 69, ChapterOne 57, Persistence 12 passed. Project has concurrent work, so suite totals differ from first pass.
- Initial Play reached the native 888.vip page, then a concurrent script reload interrupted the fixture. Saved partial report separately; not counted as a pass.
- Live RED exposed inactive WindowManager lookup (the browser is initially inactive): includeInactive=true now used in navigation and QA. A fresh bounded QA run uses existing ChapterOneQaReloadGuard to defer reloads for at most 100 seconds, releasing on completion/exit.

## Authorized scope extension — Blackjack
User explicitly requested 21点 and an open-source search during implementation.
- Fifth game: single deck shuffled each hand; hit/stand/double; A=1/11, face cards=10, dealer S17, natural 3:2, ordinary 1:1, push returns stake; no split/insurance.
- Adapted scoring/shuffle from joaokucera/unity-blackjack (MIT), pinned revision 0780e7c1873d2adbd6dfd57f896af6da6ceaafba. Full LICENSE + source/change notice in Assets/LingGuangV05/ThirdParty/Blackjack.
- Persist whole pending hand and deck position, deduct stake at deal, settle once. Closing/reloading cannot discard or reroll the hand; other gambling bets wait until it is finished.
- Files: XingGuang/Core/XgBlackjackRules.cs; DesktopBridge/Casino/CasinoPage.Blackjack.cs; XingGuang/Tests/Editor/XgBlackjackTests.cs. Existing casino state/view adapted, no native scenes/assets imported.
- Blackjack RED: missing APIs. 8 new blackjack tests + 22 casino tests pass, including real JSON pending-hand continuation, soft 17, multiple aces, natural/push, double/debit/one-card, bust, duplicate actions, full 52-card deck.
- First complete 4-game Play QA: 15 pass / 4 fail; frozen-fixture Clock advanced in click callback before 10 Hz UI refresh, suppressing alternating bets. Fixed fixture to advance Clock one action/frame before clicking. The new run must replace this result before completion claims.
- Current evidence/status: Evidence/Casino/verification.md. Do not treat prior screenshots or 15/4 Play run as final approval.
- Waiting for user to pause concurrent Unity manipulation. Main file was saved concurrently with temporary useDiskSave=0; original true must be restored via Unity without reverting other scene changes. Player save hashes remain unchanged.

## Final resolution
Previous shared-editor blockers are resolved. Cold-start Main casino flow passed 26/26, subsequent win/deal/drag feedback passed 12/12, real keyboard 888.vip entry verified. Unity casino/motion focused tests passed 40/40; latest full pure suite 533/533. Main and player saves unchanged during final QA; original disk-saving flag restored true. See Evidence/CasinoJuice/verification.md for actual evidence and retained historical failures.
