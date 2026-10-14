# Native action mini-games: implementation report

Date: 2026-10-12. Scope: seven IDs owned by the action worker. No scene changes, package installation, commits, or Unity launches by this worker.

## Files

- `Assets/LingGuangV05/XingGuang/Desktop/MiniGames/XgActionGames.cs`: factory, seven native uGUI games, shared native mesh renderer.
- `Assets/LingGuangV05/XingGuang/Core/XgMiniActionRules.cs`: segment/rectangle and circle/rectangle collision; arithmetic; real raster normalization/template distance; bounded persistent teaching records on the existing partial `XgState`.
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgMiniActionTests.cs`: 12 deterministic edge-case tests.

## Retained mechanics and native adaptations

| ID | Implemented interaction | Explicit differences from HTML |
|---|---|---|
| draw | Draw multi-stroke digits on a native ink mesh; normalize actual 64×64 raster; compare 20×20 distance-to-ink features to templates and taught examples; submit once and move to next digit. Blank/dot rejected. | Uses ten bitmap templates and nearest-example distance, not browser-font templates/weighted kNN; no fabricated confidence percentage or skill-based random guess. Latest 100 actual bit-packed rasters are saved in the existing main state and restored on opening, including legacy-null repair. No live auto-submit: explicit submission prevents accidental labels mid-stroke. |
| slice | Held-mouse segment sweeps intersect ballistic moving message cards. Cut spam/false assertions, spare normal messages; combo, missed spam, 60 s timer and three mistaken cuts end the round. | No split-card particles, synthesized browser audio or direct local wallet penalties. Misses break combo and are visible but do not award idle labels. Native text replaces emoji cards. |
| swipe | Actual card dragging with 80 px threshold; short drags snap back; left/right keys and buttons; shuffled news deck, delayed companion opinion, timed combo scoring. | Ten representative headlines instead of the full browser text bank; no external/random photography or throw-away particle animation. |
| dadishu | Nine independent timed holes; randomly generated +/−/× equations; click only incorrect moles; purple AI equations depend on saved skill; expiry/miss/combo and escalating tempo. | Native equation/hole cards instead of cartoon DOM mole/hammer animations. Correct moles left idle award nothing, intentionally avoiding passive settlement. |
| danmushooter | Continuous 2D ship movement, held fire, real projectiles/segment hit tests, moving good/bad comments, five-point host health, skill-dependent purple suggestions and wing support. 60 s waves, explicit next-wave continuation, health recovery and speed escalation. | Eight lanes and reduced text bank. Wing fires only during active player firing and never settles player labels; idle good-comment passage and perfect-idle waves do not yield currency/samples. |
| spidersnake | Smooth directional/pointer steering, word collision collection, growing spatial trail, obstacle and self collision, half-tail loss and border reflection; repopulates pages after all words are read. Starts moving only after player input. | Compact procedural word page replaces browser encyclopedia layout. Fixed advertisements, no pop-up ad animation or automatic companion radar harvesting; saved skill affects movement speed. Collision also costs 3 s; 90 s rounds. |
| zhaobutong | Two procedurally drawn native desk scenes, five randomly assigned missing/color/move/size changes. Click either image; same found object cannot settle twice. Purple companion suggestions may be false and require human verification. Timed failure and wrong-click penalties. | One desk scene with eight native vector objects rather than two rich browser scenes. No imperceptible “ghost” difference, audio, or animated circles. Uses rectangular verification outlines. |

## Settlement and lifecycle safeguards

- All `C.Report` calls are through `Result(correct)` with units=1, triggered only by an explicit submission/click/swipe, player-fired projectile hit, or head/word collision after movement was manually initiated.
- No reward on spawn, opening, reset, suggestion display, timed correct-item expiry, passive safe passage, or companion-only hit.
- Timers advance exclusively through `Tick`; no coroutines, `Update`, background tasks, or global event subscriptions.
- `C.IsActive` guards timers and context callbacks; stale mouse-fire hold is cleared on release. Root owns switching/disposal.
- Saved digit teaching uses bounded 100 × 512-byte bit-packed rasters (about 68 KB Base64 before JSON overhead), not filesystem writes or a separate wallet.
- Stroke mesh capped at 6,000 segments per submitted image. Feature extraction is bounded O(400² + input pixels), recognition O(110 × 400), no inference API calls.

## Verification evidence

- Standalone .NET 10 assertion harness compiling actual pure-rule and test sources: **12/12 passed**. Command: `dotnet run --project /tmp/xg-action-rule-check/check.csproj`.
- Covered: fast segment crossing, parallel/outside/degenerate swipe, circle corner/tangency, blank/dot rejection, teaching changes nearest example and clones arrays, bounded teaching, saved raster bit round-trip, malformed/legacy state repair and bounds, reload into a fresh recognizer, arithmetic edge cases.
- The temporary harness supplies a minimal NUnit-compatible assertion adapter; this is **not** a Unity NUnit runner result.
- `git diff --check` on owned files: no diagnostics (new files additionally inspected directly).
- Unity compilation, runtime input checks, save-file round-trip through the actual serializer, full visual QA and human playtesting remain the root agent's validation responsibility. This worker does not claim those passed.
