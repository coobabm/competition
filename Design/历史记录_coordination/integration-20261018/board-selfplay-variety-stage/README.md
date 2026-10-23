# Lv4 nightly self-play: three additional native games

Import after `board-picture-source-stage`. This independent candidate preserves that package's Lv2 copy. `board-selfplay-variety.patch` contains 12 paths. No production edits, Unity execution, C# compilation, tests or models were run by this agent.

## What changes

- Online 22:00–06:00 sessions rotate **Gomoku → aeroplane chess → Chinese checkers → Monopoly-style**. Each active night plays up to three actual terminal rounds of one game. The existing morning pack path remains one same-game pack per completed night. Skipped offline nights do not advance or invent games.
- The three added games use their existing legal engines, genuine dice and dated chance-card rules. Their in-progress engine state/log persists with the save, and they resume from that position. Terminal traces are saved as self-play, never personal-AI completion receipts.
- Three small heuristics compete in each game's real self-play. Ludo varies capture-risk cost; checkers varies reachable-route weight; Monopoly varies cash reserve. Completed records retain the policies used and actual outcomes.
- **Only installing the resulting Files pack** aggregates bounded win/draw records, picks a smoothed preference and awards the normal capped dataset samples. The three native AI panels actually consult that learned preference at Lv4. Lv0–3 decision behavior is unchanged. Duplicate installs do not update it twice. This is bounded local heuristic learning, not a trained neural model or a guarantee of improved strength.
- Existing Gomoku coordinate-bias learning is preserved. Old saves without `boardSelfGame` continue their current Gomoku night. Future nights enter the four-game rotation.

## Authored checks, pending root execution

`XgBoardSelfPlayVarietyTests` has 8 NUnit cases: three real-engine terminal/replay/install cases, three save/reload/daytime cases, rotation/no-offline-completion, and unlocked learned-policy selection. The terminal tests replay complete logs through legal engine operations and verify exact log equality. They do not inject terminal outcomes.

Static delimiter audit and both baseline/production `git apply --check` passed. Manifest reports no baseline drift at packaging time. Root must run compilation/core tests serially and verify the native panels continue to work.

## Remaining design gap

This closes the three additional native games' self-play path. It does **not** implement the separately authored xiangqi “set up and solve endgames” or chess-mate “create its own puzzles” abilities. Current production has no xiangqi game engine/page; chess mates are Teach packs using an existing verified puzzle bank. Those require separate work. Go and ordinary full chess remain the pre-existing games; the authored Lv4 table specifically names xiangqi/endgame and chess-mate instead. Do not claim all board-game Lv4 promises complete from this patch.
