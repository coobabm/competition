# Chinese checkers native candidate

Depends on imported board-ludo package. Adds pure engine, native panel and Game Hub registration; preserves original games. Shared stage-2 personal-AI/opponent selection, recorder purchase snapshot, completed-session receipts and manual pack installation remain in the imported shared bridge.

Authored rules: two players, six pieces, adjacent step or full chain jump over any occupied adjacent hole into empty landing; no captures; first six in opposite camp wins. At 40 turns each compare pieces home, with explicit draw on equal counts. A pass is legal only when no legal move exists. Necessary default-A topology choice: 73-hole six-point star with three-row arms (six holes in each camp). No forced timer; six-minute duration remains unmeasured.

Level 0 prefers steps, level 1 advances and jumps, level 3 evaluates reachable team continuation on resulting positions. No claim of screenshot recognition or self-play; those consumers remain separate packages. Full selected chain route is recorded. Hiding or rule overlay blocks native play, changing opponent/save restarts via shared bridge.

Six NUnit test cases are authored but NOT EXECUTED. Only packaging, lexical delimiter audit and git apply --check are performed here. Unity compile, tests and real native visual/input QA are root-owned and pending. No production file or shared QA runner is edited.
