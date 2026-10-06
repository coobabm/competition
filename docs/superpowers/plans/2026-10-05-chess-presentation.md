# Chess Presentation and Fan Shutdown

**Goal:** Preserve existing legal chess rules and desktop integration, replace textual tokens with original friendly 2D sprites, add legal move markers/dragging/check and capture/win feedback, and stop fan sound 10 seconds after gameplay entry.

**Boundaries:** No changes to ChessGame or gameplay economy. Reuse XgJuice with LocalHitStop=true; never freeze shared time. Respect reduced motion, hidden tabs, lost focus, and the real LCD input relay. Use existing audio and new original bitmap art; add no packages or paid API calls.

- [x] Fan: explicit gameplay countdown, 9.6–10s fade, stop at 10s; preserve startup.
- [x] Art: transparent 3x4 sheet with 12 readable original pieces; native Sprite.Create slicing.
- [x] Input: click and drop share LegalMoves; only IDropHandler commits; cancel/EndDrag never commits.
- [x] Presentation: checked king tremor, empty-square dots/capture rings, scoped white flash, knockback, squash/stretch, floats, particles, local hit-stop, board shake, shockwave, trails/glow/audio, cosmetic capture combos, win celebration.
- [x] Validate: focused unit tests, existing chess rules, real LCD Play input (legal/illegal/drag/cancel/check/capture/victory/new game), fan deadline and screenshots. Preserve player saves.

Evidence: 64/64 EditMode; 51/51 Editor Play. Documentation/Chess/README.md records scope, paths and limitations.
