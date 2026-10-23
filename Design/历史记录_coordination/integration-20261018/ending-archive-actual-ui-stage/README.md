# Ending archive actual UI candidate

Status: staged only. No Unity, Player, C#/.NET compilation, model, audio, or runtime tests were run by this agent. Static delimiter checks and production `git apply --check` are the only validation performed here. Runtime pass/fail belongs to the serial Unity owner.

Follow-up release reviews: [exact generated-asset audit](RELEASE_ASSET_AUDIT.md) and [current seven-document code-gap audit](RELEASE_CODE_GAPS.md). These are read-only reviews, not additional runtime verification. The first actual archive report and its one-line bootstrap correction are documented separately in `../ending-archive-main-bootstrap-stage/README.md`.

## Import and entry

Apply `ending-archive-actual-ui.patch` to `competition-latest-20261016`. Five paths: one existing editor runner plus two new editor helpers and their metas. Do not reapply the earlier ending-archive implementation patch. This candidate depends on that implementation and the already imported `LcdActualUiProbe` readiness helper.

Run from clean EditMode with the serial Unity owner using `EndingArchiveActualUiRunner.Run()` or **Tools > Local Verification > Ending archive actual UI**. The runner rejects a pending separate presentation-suite request rather than changing its flag. It creates a fresh `LocalVerification/EndingArchiveActual/<UTC timestamp>/`, copies source evidence before PlayMode, owns the virtual mouse/input isolation, and restores editor settings/scenes. Cleanup or final evidence-verification failure invalidates `passed.txt` and the report's passed status.

The original full `EndingFinalRouteUiRunner` also receives a minimal terminal hook: after its real successful finish, physical power must open the title archive. It now verifies the actual title replay and all three public letters and does not force completed Main entry. The online-night and prerequisite sequence is unchanged. Do not rerun the long full route just to execute this independent archive suite.

## Original evidence and provenance

Source: `LocalVerification/EndingFinalRoute/20261018-215048-664/Save` inside the production project. The original run's report remains **failed**, due to a later native replay click after its successful completion. This candidate copies that original report without reclassifying it.

- Actual completion receipt: `epilogue:08e66e41d5ad4cf08491ca7b2a7c2208`.
- Original primary: 100354 bytes; SHA-256 `9a538c1ae24be0ae4d250f608437628ed149ae5025b9c8d79fd2c05ec162415b`.
- Actual pre-choice source: `slots/4/chapter-one.sav`, 52022 bytes; SHA-256 `0acbee5e296279e0fbe0f44b62f865eaeda1c74b4ffe2c8e2d48e897ff5c5be8`.
- Shared actual memory review receipt: `memory-review:9f6c1efda35d480c80dab8f6dcadc4b9`.
- Actual SI read receipt: `si-read:306ba2931e71459a9ea23c5a476e68e8`.
- Actual final-page receipt: `final-page:0a4fa1f40ace4ef3a3f7d3c066291c27`.

The source's six-sheet answers, final-page input, SI/memory confirmations, route, publication, natural online night, native letter reading and completion were earned by that earlier UI run. Its stage/fullOpen state, five historical AI replies and two teaching-history records were declared upstream fixtures. This suite makes no claim of natural early-game progression or new model inference.

The entire source file tree, including manual slots and any snapshot blocks, is copied separately to immutable `EvidenceCopy` and working `Save`. Tree digests include relative filenames and every file's SHA-256; reparse points are rejected. The original and immutable copy are rechecked before success and after leaving PlayMode. Nothing writes to the original source evidence. Normal startup `lastSeenUnix` updates may occur in the working copy outside the measured read-only archive interval.

## Actual coverage to execute

1. Start from the copied actual completed primary in the physical title. Queued mouse power opens Endings. Actual replay controls advance through the saved presentation and open the public letters. All three article titles and nine paragraphs per letter must match resources; actual Back controls return to the archive. The complete primary payload, house/lab state, identity, Saving count and CompletionSaved count must remain unchanged. The old completed source has no new automatic checkpoint, so branch creation must be disabled rather than fabricated.
2. Explicit lifecycle fixture: copy the actual source slot-4 payload into the separate working primary and load Main. For online, public model, deterministic Ask it, and local-only choices, reload that real pre-choice slot and click actual preparation controls. Inject collection failure and then main-save failure after checkpoint creation; assert rollback of primary, preparation, current identity and association, with real retry controls enabled. Successful retry must durably associate a fresh before-choice checkpoint with the selected route. Ordinary subsequent saves must not overwrite it. Ask it must use the actual deterministic result and source-memory citations.
3. The last route is local only. Click its three actual pin controls and permanent-pin/release confirmations; no online night is started, shortened, or rerun. Reuse the real native letter save/open/read/failure/retry checks, then the real completion button and production return-to-title flow to earn a new local completion receipt and retain its genuine automatic pre-choice checkpoint.
4. Repeat the read-only title replay/three-letter checks for the newly earned completion in Chinese and English.
5. Click the real independent-branch button first with a private destination obstruction fixture, assert completed primary/identity preserved, then retry. Assert actual store/Sim switch, correct pre-choice branch payload, and unchanged primary/manual slots/checkpoint tree. Save an explicit branch-only `money += 7` marker. Invoke the production shutdown command and await fresh title; original primary must be the default. Use actual power, branch-list and branch-row controls to resume the independent branch and its saved marker without replacing the primary.

Title controls use queued InputSystem mouse events and exact real EventSystem hit checks, never direct `onClick.Invoke`. Main controls use the actual LCD relay after `LcdActualUiProbe.WaitForClick`. Whole-title PNGs must decode, match real screen dimensions and be nonblank.

## Explicit limits

- Reloading the actual source slot/working primary and loading Main is an explicit lifecycle fixture.
- Upstream history remains a declared fixture. Economy/lab ticks, model services and unrelated story drivers are paused.
- Public-letter scroll position is a ScrollRect geometry fixture, not an OS-wheel claim. Native completion reading uses the existing native letter checks.
- The branch money marker and private destination obstruction are explicit persistence/failure fixtures.
- `GameExit.ReturnToTitle()` is a production-command invocation fixture, not a claim of clicking the desktop shutdown control. The branch selection and resumption themselves use actual UI.
- This suite verifies title replay. Native mailbox replay Close/Minimize and the completed-presentation receipt-observer race are **not run here** and must be reported as unrun until the separate completed-native replay suite runs. The full-route report explicitly has the same coverage split; title archive results are not a replacement claim for those cases.
- For the separate short native suite, the peer's source-evidence patch adds `EndingPresentationActualUiChecks.RunCompletedReplayFromEvidence(reportPath, saveDirectory = null)`, or the existing `RunCompletedReplay` execute method plus `-endingReplaySourceReport <report.json>` and optional `-endingReplaySourceSave <dir>`. Its source validator requires the full route's natural-40-request/native-completion evidence, so use the real full-route report, not this independent local-only suite's report. Do not repeat a natural online night to obtain another completion.

`manifest.json` records baseline and candidate hashes, baseline drift, and the absence of runtime tests. `package.py` is rerunnable; it repackages the candidate without regenerating or overwriting it.
