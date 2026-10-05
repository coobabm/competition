# Casino win feedback and dealing implementation plan

Goal: casino-local win feedback (white flash, knockback, squash/stretch, floaters, particles, local hit-stop, shake, shockwave, trails/drag, glow, audio, streak) plus sequential physical card dealing. No payout/rule/economy changes.
Architecture: reuse XgJuice/XgRingGraphic/XgGlow/Sfx. Add opt-in local hit-stop and squash so existing callers keep their behavior. Persistent audio-host reuse avoids tab close destroying the shared SFX pool. CasinoPage owns pending presentation only; core settles/saves exactly as before. CasinoCardMotion owns bounded deal/flip/trail/drag-return animations; new cards start face-down at the shoe, existing cards stay put.

Constraints:
- Shared dirty project /Users/kk/competition; do not stop another session's Play/tests, discard its changes, overwrite saves, or hand-edit scene YAML.
- Time.timeScale never changes for casino wins. Static outcome replay/opening old tabs cannot replay celebration. Reduced FX/mute honored.
- Blackjack initial order player/dealer/player/dealer, then one card per hit; dealer hole flips before extra dealer draws. Hide result/score spoilers until motion ends; animation only reads already-committed results.
- Busy guards on actions during travel/flip/drag; interrupted/closed tabs discard presentation but never refund or award money. Reopening reads saved hand.
- Motion and particles capped; no new dependencies/assets or paid generation. New source/meta only.

Tasks:
- [x] RED/GREEN pure presentation rules: true net win vs push/loss, consecutive wins (capped at history window), stable deal sequencing.
- [x] RED/GREEN Unity reflection tests: local hit-stop leaves global scale unchanged; deal begins at shoe, has intermediate position, finishes/turns face-up; hidden hole remains down; reset cleanup.
- [x] Minimal XgJuice reuse hooks (local hit-stop, squash, persistent sound pool), implement CasinoCardMotion and CasinoPage feedback integration.
- [x] Refactor blackjack rendering to animate only new/revealed cards and delay local win presentation until dealing completes; add dragon/tiger deal motion too.
- [x] Run pure regression and compile; focused Unity tests and isolated Play with midpoint/landing/win screenshots when shared editor becomes available. Carry forward unresolved previous casino QA/save-toggle cleanup.
- [x] Independent read-only review, document actual verified boundary. No push/commit requested.

Acceptance: visual components alone are not proof of motion. Test progress and intermediate transforms, end pose and hidden-card reveal; no global pause, no duplicated credits, no stale cached celebration, no stuck input after closing/reopening.

Progress:
- Pure presentation rules RED missing APIs -> GREEN; casino/blackjack/presentation 33/33, complete XingGuang 533/533.
- Existing shared Play was not stopped. Isolated temporary-component probes ran without accessing player simulation: local hit-stop passed (global timeScale stayed 1); card travel exposed Unity native fake-null with `GetComponent<CanvasGroup>() ??`. Fixed to explicit Unity null comparison; original red captured under Evidence/CasinoJuice/motion-red.json.
- Added lifecycle tests for disable-mid-flight, hole delay/flip, queued-payout cancellation and persistent audio pool reuse. Pure tests are not visual QA.
- Minimized-window visibility now cancels queued payout and card drag/flight state; stale celebrations and stuck inputs must not resume on restore.
- Added isolated Main fixture feedback pass (`CasinoPlayQa.Begin(true)` chains `CasinoJuicePlayQa`): actual LCD roulette wins, local clock check, floaters, sequential blackjack midpoint/landing, mapped card drag/return, dealer flip and end-pose captures. Requires disk-save-disabled fixture; refuses a live saved game. No invocation has been counted until its report completes.
- EditMode lifecycle tests explicitly invoke ordinary MonoBehaviour OnDisable when Unity is not playing; they do not pretend EditMode dispatches runtime callbacks. In Play the same tests use actual enable/disable dispatch.
- A focused Unity run used stale loaded assemblies because the new Editor QA file had not been imported (missing .meta; compile error CasinoJuicePlayQa not found). It reported the two pre-fix EditMode-lifecycle failures. Not counted green. Importing the exact new QA file and updated test file before rerunning.
- Fresh focused Unity EditMode run: 40/40 passed, job ea12a70d309d4c96968427e97a156bf9 (saved unity-tests.json).
- Main casino Play flow completed 26/26 with five games, actual pointer clicks, pending-hand persistence, multi-tab, advert and English checks (base-flow-26-pass.json).
- Cold native browser revealed DreamOS Start.disableAtStart closing the first OpenWindow: casino navigation now warms inactive object for one frame before explicit OpenWindow. No vendor script modified.
- Chained feedback fixture attempted to click while the locale change's restored-hand animation was still busy; updated to wait for card motion completion rather than fixed delays. This is not counted as a feedback pass; original failure retained.

Final validation:
- Full pure suite 533/533; focused Unity EditMode 40/40.
- Cold-start Main casino flow 26/26; feedback/deal/drag flow 12/12, repeated successfully after clean native keyboard URL entry.
- Screenshots visually inspected: impact flash/shockwave/profit, card-flight midpoint/trails, final blackjack table. Both real pointer drag/return and hidden-card reveal completed. Global timeScale stayed 1 and wallet settled once.
- Evidence and boundaries: Evidence/CasinoJuice/verification.md. Prior failed attempts retained as red evidence.
- Cleanup complete: stopped own Play, restored useDiskSave=true, compiler clean, scene not dirty, Main/player-save hashes unchanged. No commit/push.
