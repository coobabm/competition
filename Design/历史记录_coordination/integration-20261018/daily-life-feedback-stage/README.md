# C7 daily feedback candidate

Candidate only. Existing C7 daily integration is retained. Nothing in production, XgDailyQuestions, GameHub, new board games, side-game logic or the shared runner was edited. No Unity/.NET compile, model call or playtest was run by this agent.

## Import

Apply `daily-life-feedback.patch` at the production project root and copy the new files from `candidate/`. The patch contains only small changes to `GameMusic.cs`, `XgSim.DailyLife.cs`, `XingGuangController.cs`, `XgTechTreeCopy.cs` and `XgTrainPage.cs`. In particular, shared GameMusic and DailyLife changes are supplied as minimal hunks, not replacement files. `baseline/` and `working/` are audit inputs, not import directories. `SHA256SUMS.txt` covers the patch, inventories and candidate files.

## Behavior

Seven daily nodes now have explicit three-line name/summary/effect cards and consumer-specific Details text. `bilingual-card-copy.json` lists the seven cards; `bilingual-all-copy.json` lists all 32 new bilingual pairs including details, notice and rhythm cue text. Cards no longer fall through to internal-looking data names or copy/effect-needs-review placeholders.

Rhythm is consumed by the real `XgTrainPage.OnEpoch` completion event. Results, score changes, assessment and saves happen immediately as before. Only its completion pulse may wait, at most one second, for an actual energy rise in the already-playing GameMusic AudioSource. It reads a fixed caller-owned 256-sample buffer at most about 30 times per second while one cue is pending. The small RMS detector requires warm-up, a rising signal above its own recent baseline and a refractory interval. It does not estimate a BPM, create a beat clock, seek or alter songs, start music, add voice or change playlist order.

Silence, unavailable/muted/paused audio, a changed track, a seek, no clear accent within the bound, or reduced effects uses the existing ordinary completion feedback. When the rhythm branch cannot detect an accent it explicitly says “本轮完成 · 无清晰重音，使用普通提示” / “Round complete · no clear accent; normal cue”. Reduced effects uses the existing reduced-motion behavior without audio sampling. Automatic rounds coalesce a pending cue, and cue text is limited to one per five seconds for automatic rounds. Hidden/changed-save/changed-track stale outcomes are discarded. There are no added full-scene searches or allocations in the sample/tick path.

New real daily receipt transitions emit a core event exactly at the corresponding node's threshold. A notifier subscribed to the exact bound simulation waits until the node is available at the current stage, records its delivery acknowledgement through real SaveNow, then requests the existing optional tray notification. Clicking opens that exact technology's Details page without purchasing it. Failed saves roll back the acknowledgement and retry at most once per second. Switching saves drops the pending queue and stale callbacks check both owner identities. No existing completed conditions are scanned to synthesize notices on load; losing an unsaved pending prompt across a reload is preferred to backfilling history.

Birthdays still require an actual received Qingwen message, using the existing chat-owner check. The affirmative text predicate is tightened to a direct declarative sentence so “if my birthday…”, quoted examples, and question forms do not qualify. This deliberately accepts a narrow wording; it does not infer a date from biography, calendar constants or unrelated chat. Anniversary dates have no recorded source in the inspected path, so no anniversary reminder is invented.

## Explicit remaining gaps

- Risk forecast: `DailyRiskEnhancementAvailable(actualRiskForecastOwned)` exists, but there is no independent risk-forecast consumer in the inspected code. This package does not claim or advertise that enhancement as implemented. The probability question desk remains the actual consumer.
- Anniversary reminders: no authoritative disclosed date source found; no implementation or completed claim.
- The energy detector finds audible-output accents, not guaranteed musical beat tracking or tempo estimation. A real music/screenshot run is required before asserting a detected-accent result.
- Notifications are optional best-effort presentation after a saved acknowledgement. This is not an exactly-once external delivery system, and no old-save backfill is performed.

## Checks prepared, not executed

`XgDailyFeedbackTests` adds eight NUnit checks: new threshold events, no old-save backfill, duplicate/invalid evidence, persisted acknowledgement eligibility, three-line bilingual coverage, energy-rise versus constant/silent audio, invalid/gapped samples, risk-gate honesty, and affirmative birthday forms (some checks combine related assertions). Existing C7 integration tests should still run.

`DailyRhythmActualUiChecks.Frames(ctl, language, actualLCDclick, capture)` is a new editor-only helper for root's existing isolated, powered real-training fixture. It requires Rhythm already purchased and normal effects; it creates no paid-node or training-result fixtures itself. It clicks the actual Train button in both languages, waits for a real completed round, then checks the visible detected-accent or explicit fallback cue. It only claims the branch actually observed. The root should run it once with actual GameMusic playing and once unavailable/paused. It does not edit the shared runner or start a model.

Root's additional integration checks:

1. All seven card panels in Chinese and English have three readable short lines, Details match actual consumers, and no internal IDs/review placeholders appear. Check the narrowest supported tech-card size.
2. Train with Rhythm unowned, then bought: ordinary feedback before purchase; real music accents or truthful fallback after purchase; same score/epoch/result timing. Pause/mute/switch/seek music during the one-second pending window, switch tabs/saves and enable reduced effects. No delayed old cue or extra voice; repeat/shuffle defaults remain untouched.
3. In an isolated save, perform a fresh third qualifying game or third complete distinct song. Require one saved notice acknowledgement and one optional tray action to the exact daily node. Replayed receipts, old-completed save reloads and purchased nodes must not emit a new prompt. Inject actual Saving failure, require no notice before successful retry, then click the notice after switching saves to verify stale callback rejection.
4. Birthday display requires the saved Qingwen sentence, and stays absent for questions/quotes/undisclosed history. Do not treat the pending anniversary or risk consumer as a pass.

All runtime/UI evidence is still pending root's serial Unity validation. No pass counts or screenshots are claimed here.
