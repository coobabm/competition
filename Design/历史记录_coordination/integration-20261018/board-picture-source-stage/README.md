# Public board picture → purchased Lv2 reader

Independent staging only. Import `board-picture-source.patch` against `competition-latest-20261016` after the board-logic package. No production files edited, no Unity/C# compile/model execution performed here.

## Result

- Bodu home and searches for 五子棋 / 棋盘 / gomoku / board expose an original Lane Chess Club practice page. Tieba home exposes a public board-game post. Both use the existing `DesktopMedia.Picture/Paint` image mechanism.
- Original 2016-05-21 board artwork is rasterized locally from a legal alternating eight-ply position. It is authored game content, not an external copyrighted screenshot. No resource binary/importer dependency is added.
- A Game Hub sidebar entry and independent reader page appear only after buying logic Lv2; no next-move desk purchase is required. The separate ¥1000 question desk remains separately gated.
- Dragging the actual image between native windows captures its rendered texture pixels. The reader receives RGB bytes only: no source ID, answer, replay, move metadata or board-array shortcut. It reconstructs cells then calls the existing move solver. UI states the narrow standard 240×240 / 15×15 local-image scope, Black to move, A–O / 1–15 top-down coordinates.
- Source and target validate active window/page visibility and the current `XgSim`; source additionally snapshots its `XgState`. Disabling a source cancels it. Each payload is consumed once. The existing inverted-UV Teach source uses the same path, with pixel row normalization.
- The forum post has no knowledge of player AI, and creates no player post. Reading creates no games, samples or data packs.

## Static validation performed

`python package.py`: 18 patch paths, balanced C# delimiters, no baseline drift.

`git -C competition-latest-20261016 apply --check ../.coordination/integration-20261018/board-picture-source-stage/board-picture-source.patch`: passed.

`BoardPictureDropQa.Run` is authored for root's serial Editor execution. It checks the public image's actual pixels and orientation, F6 independently computed by the solver, corrupt-pixel rejection, existing RawImage orientation, unpurchased gate, hidden source/target, once-only consumption, stale source/target across saves, same-Sim state replacement, and absence of rewards. This lightweight handler QA does **not** claim actual Main-scene pointer/raycast or screenshot verification.

## Main UI checks still to run by root

1. Without Lv2, Game Hub has no reader entry; original six games remain accessible.
2. With Lv2, open Game Hub reader; open Bodu → chess club (or search gomoku). Position windows so the original image and drop panel are both visible. Drag image onto reader. Expect Black F6 and eight recognized stones.
3. Repeat using Tieba home → board-game post; repeat in English. Check text containment and actual image rendering at native sizes.
4. Start a drag, switch save, and attempt to drop; result must stay empty. Hide source/target while dragging and verify rejection.
5. Existing purchased Next Move question desk still accepts its own inverted-UV image and these public originals; dropping an unrelated standard board only changes its prediction, never the authored question answer.

## Lv4 source audit

`Design/data/board_games.json` explicitly specifies self-play for Gomoku, Ludo, Chinese checkers and Monopoly; xiangqi must set up and solve positions, and chessmate must create puzzles. The overall design says nightly self-play plus morning record packs. Neither source explicitly permits implementing only one game. The previous board-logic package's Gomoku-only nightly loop remains a known partial implementation, pending a separate candidate. The user's “any game type” permission concerns the **three completed personal-AI games purchase prerequisite**, not the feature coverage of Lv4.
