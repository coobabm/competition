# Ending presentation candidate — static only

Staging only. No production file was changed; no Unity, .NET or model inference was run. Apply only the minimal hunks in `ending-presentation.patch`, then copy only the paths in `new-files.json`. Existing-file candidate copies are review artifacts, never whole-file replacement instructions. Other agents may add public-letter archive hunks in the same director/bridge files.

## Behavior

- F3/F4: normal date phase uses a 4-second fade/interval followed by 3 seconds of retained calendar month flips to 2017-06-13. Historical-review mode uses a 1-second static target fade rather than moving the displayed chronology backwards. The simulation calendar and receipts are untouched. Headline fades in over 0.8 seconds.
- F6: up to three source-gated public discussion excerpts enter over 2 seconds. Selection reuses the existing public catalog/evidence gate; browser/archive excerpts do not imply native application delivery or reading. A no-public-event route falls back to approved historical records. Reduced effects show static date/headline/cards immediately.
- Actual Skip settles the current page without advancing the persisted phase. Continue advances only on the next click. Existing actual-letter and completed replay helpers now wait the actual `TransitionPlaying` property between phase clicks.
- F10: the existing `Complete` implementation and immediate `CompletionSaved` durable event are unchanged. The bridge captures the exact post-save owner/revision/receipt and delays only title routing for the actual closure visual (3 seconds; independent 4-second routing deadline). Hide/disabled/destroyed view clears the visual wait; save replacement discards pending routing. Return to title skips the display interval. Reduced effects show static text immediately while retaining a bounded 3-second reading interval.
- The white page shows actual final-page lines only when linked to the saved trial-line source. Its optional red shutdown rule comes only from the existing approved resource and exact pinned-card/prerequisite receipt. It never invents an unpinned memory or uses legacy ending inference.

## Run under the root's serial Unity owner

Menu: **Tools / Local Verification / Ending presentation actual UI**

Execute method: `EndingPresentationActualUiChecks.Run`

This reuses `EndingFinalRouteUiRunner` and its 900-second deadline. Output remains `LocalVerification/EndingFinalRoute/<timestamp>/report.json`, actual screenshots and `passed.txt` only if the complete runner succeeds. Read `presentationScope` in the report. This package contains runnable assertions, not passing runtime evidence.

The runner retains its explicit stage-6/startup/history fixtures and earns preparation/publication/night/letter receipts through the existing production UI suite. Neither those fixtures nor the presentation extension certify natural early-stage game progression or model inference. Its two natural 40-request nights remain real timed waits. Existing failed artifacts remain failed.

Presentation extension assertions cover bilingual natural date motion, actual LCD Skip, static reduced date, natural headline/three-card traversal, unchanged simulation date/receipts/disk during display-only actions, actual successful-save white text fade and delayed native title return, and completed-save replay Skip/Hide/view disable/destroy/reduced reading interval. Reduced preference changes are explicitly assigned UI fixtures, not clicks on the settings control.

For the routing race, the extension deliberately calls the bridge's completion observer with the already durable completion receipt, then swaps to an unrelated explicit in-memory save before the next frame. This tests pending-notification ownership; it does not emit another CompletionSaved, invoke Complete, or manufacture a completion receipt. It checks that no title return occurs after the deadline, then restores the actual completed disk archive. Direct completed-visual replay checks do not claim a second natural completion.

## Remaining boundaries / integration review

- No audio change: the existing case fan already fades/stops during normal boot and exposes no owner-safe epilogue volume hook. This candidate does not restart audio or claim fan/music fade coverage.
- No new unpinned red-rule prose; no private appendix or implied private publication.
- Public-letter archive, title ending archive/checkpoint, manifest/version/checksum UI and notification gates are outside this candidate.
- Root must compile and run serially, inspect Chinese/English date/card/white-page screenshots at the LCD resolution, including long final lines and red-rule fit. No layout or runtime pass is claimed here.
- The presentation-specific run directly verifies normal-effects successful-save title routing; reduced closure is checked as a saved-receipt visual replay, not a separate reduced-effects end-to-end title route.

`prepare_patch.py` regenerates the patch and metadata from the already reviewed staging baseline/candidate snapshots; it never reads or writes production. `manifest.json` records original baseline byte hashes and normalized content hashes. `SHA256SUMS` records candidate files and the minimal patch.
