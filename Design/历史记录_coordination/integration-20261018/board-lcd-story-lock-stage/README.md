# Board QA: isolate the delayed YY join cutscene

One Editor QA file only. No production, board state, rule, outcome, LCD bridge, camera or save changes. No Unity/Player/build/C# tests/models/audio executed.

## What the failed evidence proves

BoardGamesUI/20261018-233417-939 remains failed. It completed real Ludo and Chinese checkers and entered actual Monopoly; multiple Roll and Buy property clicks passed top-hit and LCD mapping checks. The last Roll fails in LcdSurface before an input event, not in Monopoly game logic. The old condition combines absent bridge/surface, inactive surface and inputEnabled=false into one message, so the exact failing predicate and lock owner were not recorded.

The log contains Prologue title at startup, followed by the successful native board inputs; no intermediate title-scene/Main reload is logged before failure. It therefore does not support claiming the title/prologue reset caused this later failure. The captured source frames prove the LCD previously worked, not its state at failure. There is no failure frame.

## Confirmed isolation gap and bounded attribution

StoryDesktopPresenter.PrepareDesktopShell installs AiJoinsYy as a separate component. Disabling the presenter does not disable that component. Existing StopStory excludes AiJoinsYy. YYChatHub.CheckLingGuangUnlock repeatedly requests that cutscene for an installed app, stage>=1, non-prologue and lingguangUnlocked=false. AiJoinsYy has ForceAfter=90 seconds and can then bypass normal quiet gates. Begin calls Block, which directly disables the actual LCD input.

The first screenshot was written at 07:34:45 local and failure result at 07:36:12, about 87 seconds later; fixture construction precedes the screenshot. The post-failure save still has stage2/prologue2/installed app and lingguangUnlocked=false; epiphany=false excludes the normal AutoLabel pending prerequisite. These facts strongly fit the delayed join cutscene, but do not uniquely prove the runtime lock owner. Post-failure save also includes an additional Monopoly completion; teardown can continue gameplay, so that record is not promoted to a successful QA round assertion.

## Minimal candidate

Add only AiJoinsYy to the existing explicit story-host isolation in OnScene and fixture Boot; its OnDisable unregisters its join delegate and releases only its own held input. This is a prepared-desktop fixture boundary, not a game-rule fix. No flags claiming the story was played/read are written. Keep the actual LCD readiness predicate unchanged. On failure, record each predicate plus source camera, LCD power/transition, EventSystem, prologue, AiJoinsYy/AutoLabel and title display state. The diagnostics observe only; they never turn input back on, restore a camera, sleep past a lock or bypass a real hit.

Static delimiter/baseline checks and git apply --check only. Serial owner should rerun RunWithLogicTools. A pass would verify this fixture correction, not retroactively establish the historical lock owner or certify cutscene/game coexistence. If it still fails, the new diagnostics distinguish lifecycle/power from story input locking without weakening LCD assertions.
