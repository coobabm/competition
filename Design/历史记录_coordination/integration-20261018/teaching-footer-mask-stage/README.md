# Teaching footer mask: minimal product candidate

Staged only. Production, the QA runner, game rules and font resources were not edited. No Unity, Player, .NET, model or build was started. `teaching-footer-mask.patch` passed plain `git apply --check` against the current production file.

## Actual failing evidence

Read `LocalVerification/TeachingFullRounds/20261018-232633-667/report.json` and visually inspected `spam_swipe-zh-correct.png`. The actual run failed on `Footer outside active mask: LastAiAnswer` after the first successful Chinese `spam_swipe` round. The player footer passed its text/rect check before the AI footer failed the parent-mask containment check. The screenshot shows the next question already active, the retained player line, the AI line clipped at the bottom edge, and the combo animation still in progress.

This does not establish that the remaining 18 packs or English passed. The report explicitly uses Unity Button callbacks, not actual LCD pointer input. The old failed report and screenshot remain unchanged.

## Mechanism

- `XgPackQuestion.Header` creates separate player and AI footer text at y=437/h=18 and y=455/h=19 on the 900×480 stage. Their rest-position bottom clearances are 25 and 6 stage units.
- `XgDeskGamePage.Fit` sizes a fixed `GameViewport` RectMask2D to the stage's resting size.
- Successful settlement calls `Fx.ComboHit(..., surface, ...)`, using the entire stage as the struck visual.
- `XgJuice` transforms that stage with `scale * (1 - amp*wobble)` and `position + direction*wobble`. Its damped sine has a negative overshoot. The footer inherits expansion/downward rebound while the ancestor viewport mask remains fixed.
- At combo 1 and illustrative stage scale 0.7, the existing equation gives the player line about +7.65 pixels of bottom clearance, while the AI line reaches about -5.93 pixels. This reproduces the observed ordering of player-pass/AI-mask-failure. `static-geometry.json` is an independent calculation, not measured Unity transforms or a runtime pass.

## One-file change

`Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgDeskGamePage.cs`: when `game is XgPackQuestion`, pass the current active `SocialBackground` rect as the struck visual. Other minigames keep the original stage target. The lookup runs only on a successful settled answer and ignores inactive old backgrounds awaiting deferred destruction.

Both retained footer lines and the next question's hit rectangles remain siblings of that background under the stable stage. The background retains its white flash/punch; combo audio, particles, shockwave, counter trail, settlement press and other feedback remain in the existing ComboHit path. A question subview without that background uses a null struck visual, which FlashWhite/Knock already handle; it never falls back to punching the shared stage.

No text is removed, shortened or rewritten. No font size, autosize bound, layout coordinate or mask is changed. The existing next-frame question advance and independent pre-submission AI/player outcome capture are unchanged. The candidate does not add a feedback dwell or delay the QA until animation ends.

Wrapping/reparenting the entire question tree was deliberately avoided: digit/shift and captcha code contains direct `C.Root` child lookups. Retargeting the decorative visual preserves those existing object relationships.

## Validation still required by the parent

Run the unchanged TeachingFullRoundChecks across its Chinese/English 19-pack scope. Inspect the first correct and wrong swipe screenshots while the next question is active, then the other packs, and retain text/glyph/rectangle/mask assertions. Check normal and reduced-motion paths as relevant. This candidate fixes the verified mask-boundary mechanism; it does not claim that previously unreached long English explanations or other pack layouts have passed.

The previously delivered selective Git path list already contains this tracked file, so no new path is needed. Its eventual staged content must include this candidate only after the parent imports and verifies it.
