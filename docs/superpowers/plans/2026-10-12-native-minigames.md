# Native LingGuang MiniGames Implementation Plan

**Goal:** Port all 23 user-provided HTML games to native Unity uGUI in LingGuang; no browser/runtime package, no independent wallet.
**Spec:** Documentation/LingGuang-Incremental/minigames/hub.html and every linked HTML, including three modes in index.html.
**Architecture:** Reuse XgUi and IXgPageHost. Native games render into a fixed 900 x 480 stage. XgMiniGamesPage owns lifecycle and selection; XgSim.MiniGames owns gating, settlement and saved counters; mini-game classes only implement interaction. Existing special label/review/story modes remain authoritative.
**Tech Stack:** C# / Unity 6000.4.5f1 / uGUI / TMP / existing Input System.

## Constraints and decisions
- Work in user-authorized live /Users/kk/competition checkout; no reset, branch switch, commit or remote operations. Preserve preexisting dirty work.
- Scope includes all 23 games, not a list of launcher buttons or reskinned yes/no cards.
- HTML is the behavioral reference, not pixel-identical rendering; native vector/grid/text art substitutes browser emoji when necessary.
- No runtime LLM/API calls. Companion competence reads actual saved game progress/checkpoints; clearly simulated companion responses, not real model inference.
- Games pause input/timers when hidden/unfocused. Closing, switching saves or restarting must not duplicate settlement or lose main progress.
- Currency goes through IXgHost; samples through existing source/quality logic; training uses existing epochs and all their cost/blocker logic.
- Preserve offline save format with additive defaults; no external HTML localStorage import.

## Tasks
- [ ] 1. Contract/base UI, persistent catalog and bounded settlement. Test unknown/locked IDs, wrong answers, repeated claim, cross-session claim, quality blockers, training costs and save repair.
- [ ] 2. Action games: draw, slice, swipe, dadishu, danmushooter, spidersnake, zhaobutong. Each retains its interaction, timer/collision/failure rules; drawing uses actual stroke recognition/teaching.
- [ ] 3. Board games: alphago, forgetax, vramtetris, merge2048, sentence, lianliankan. Test capture/suicide, 2048 one-merge/no-op, Tetris bounds/rows, token sequence/pair matching.
- [ ] 4. Social games: esp, captcha12306, feihualing, doutu, zhaoshuijun, shuishiwodi, kaixincidian, chuanhua, turing, qingwen. Read each HTML and retain distinct mechanics, staged rounds and explanations.
- [ ] 5. Integration: native hall and desk replacement; train/data/story shortcuts; lazy create selected game, dispose and reset on save change; preserve special original flows.
- [ ] 6. Compile, deterministic domain tests, Unity EditMode and native UI/input verification; report limitations separately from passing tests.

## Shared native interface (all workers consume the actual XgMiniGame.cs)
Namespace LingGuangV05.Desktop.XingGuang.MiniGames.
Each factory implements public static XgMiniGame Create(string id, XgMiniContext context), returning null for unknown IDs.
Factories: XgActionGames, XgBoardGames, XgSocialGames. Constructors consume context; Build is called by root once, Tick only while active, Dispose on switch.
Context: Root (900x480 RectTransform), Ui, Sim, Host, Skill (0..1), Random (System.Random); Text, Button, Box, Pointer helpers; Say(string), Report(bool correct,int units=1), KeyDown/KeyHeld(Key).
Report is for verified completed interactions only, never merely opening a game, rendering, viewing a hint or resetting.
Workers own their group files only and group pure-rule tests. Root owns context, catalog, integration and settlement. No nested delegation.

## Validation evidence ledger
- Before changes: existing Tools/XingGuang/bot builds/runs but ends with existing Object reference not set to an instance of an object (baseline /tmp/lingguang-minigame-baseline.log). Do not claim full suite green from that baseline.
