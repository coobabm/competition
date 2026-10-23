# Board logic, question desk and real Gomoku self-play candidate

## Implemented

- Four ordinary-price purchases 800 / 2500 / 6000 / 15000 at stages 2 / 3 / 4 / 5. One native Tech square with four levels, independent recorder and next-move squares. Lv1 requires MLP + s.d2; Lv2 LeNet; Lv3 LSTM; Lv4 recorder and **3 actual personal-AI completed games of any kind**, not ten. No mainline dependency. Discounts remain ordinary existing discounts.
- Next move is separately bought for 1000 at stage3 after Lv1 and opens a real four-choice Teach desk. Legal candidate moves, win/block labels and actual AI move search use board positions. No accuracy-roll answer injection. The new desk does not use XgPackQuestion.Choices/Guess, which can synthesize AI answers elsewhere.
- Gomoku Lv0 now uniformly samples empty cells (the former Pick(skill=0) still sampled high-ranked runner-ups). Lv1/2 always take immediate wins or blocks; Lv3 searches an opposing reply and own continuation, with bounded top-eight candidates. Lv3 recalls player coordinate frequencies from actual previous personal-AI Gomoku rounds. Lv4 consumes bounded position priors from installed real self-play. Existing Ludo, Checkers, Monopoly and Chess consumers already read BoardLogicLevel.
- Lv2 question images use a 240x240 RGB 15x15 standard native board crop. A real drag from the picture into its reader reads pixels, classifies black/white/empty at four offsets per intersection, then computes moves from the recognized cells. It receives no puzzle answer or metadata. Unsupported formats/colours reject visibly. Notebook review preserves board15 images.
- Real incremental self-play through the current host calendar, while the application is running 22:00–06:00. At most one ply per 0.25-second tick, three actually terminal Gomoku games per night. Legal logs persist, resume after load, never generate personal-game gate receipts. No offline/catch-up/fabricated games. At the following morning a complete three-game batch becomes a virtual Files pack; **only real manual installation** adds samples and updates small bounded position preferences. Samples <= actual trace length. This is local rule search with a simple installed outcome-derived positional prior, not a claim of neural-network training or guaranteed Elo improvement.
- Rules checkbox can be re-enabled; Go launcher copy no longer promises legacy vision-driven strength.

## Explicit remaining gaps

**This package is not full acceptance of all original board-logic promises.** The pixel path is the question desk's standard Gomoku crop. Native browser/贴吧 source images and their cross-window drag-to-AI destination are not wired here; arbitrary screenshots, chess screenshots, image perspective/scale/occlusion, and neural image recognition are not implemented. Do not describe the Lv2 purchase as general screenshot understanding. Self-play currently executes Gomoku only; authored game-specific self-play for the other games is still missing. Memory is bounded coordinate-frequency recall from previous completed Gomoku games, not a general opponent model. Six-minute game durations remain unmeasured.

An interrupted night with fewer than three completed games does not issue a fake full batch; a later night starts new work. Pending interrupted-night traces remain in boardRounds as audit data. App closed overnight does not play. The host uses the same GameCalendar clock as the native desktop (story date + real time-of-day), not billing periods.

## Validation

12 NUnit cases authored, NOT EXECUTED: six pure policy/image/replay tests, six technology/desk/actual-selfplay/reload/memory tests. Lexical delimiter audit, zero baseline drift, and git apply --check passed. Unity compilation, tests, runtime performance and native visual/input QA belong to root and are pending. No production Assets or shared QA runner changed by this agent. Depends on imported board-ludo/checkers; Monopoly is independent and should also be imported.
