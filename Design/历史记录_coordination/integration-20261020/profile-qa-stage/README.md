# Profile memory QA candidate

Candidate copies only. Apply after Unity exits to the matching files under `Assets/LocalBuild/Editor/`.

## Evidence

- `ending-ui-02.log` reports the aggregate `CanOpenProfileMemories` failure; it does not record the individual live gate values.
- The actual saved `LocalVerification/SidebarLayout/20261019-174629-400/Save/chapter-one.sav` has two discovered memories, stage 6, `fullOpen=true`, no stage presentations, no shutdown dispatches/answers, no committed release, and `chapterComplete=false`. Its app-acquisition active script and pending group cold scenes are empty.
- `fixture-lab.json` is the earlier shared bootstrap fixture, not the replacement ending fixture. It should not be used as evidence of replacement fixture state.
- The shared runner runs the desktop for multiple frames before entering the ending test. The test changes `StoryFocus.Probe`, disables the controller, and rebinds a new save. None of those clears the static `InnerVoice` queue, which independently blocks `NarrativeAvailable`.
- This establishes a fixture isolation omission; the historical run's actual live blocker remains unproven until rerun because it was not logged.

## Changes

1. `EndingPrerequisitesActualUiChecks.cs`: log the prior fixture's voice Busy/Pending values and call public `InnerVoice.Clear()` immediately before binding each replacement isolated fixture. No frame is yielded between cleanup and binding. This does not close or answer a shutdown seed, change memories, or change production gates.
2. `ProfileMemoriesUiChecks.cs`: append actual gate diagnostics to the existing failing precondition, including Current/NarrativeAvailable, enabled, memory count, seed, ending panel, filter/text, prologue, minimized, StoryFocus, InnerVoice, stage presentation, pause/reading, group script, save/settings windows, epilogue and release state.

All pending-question/stale-callback assertions, the original gallery checks, unchanged-state checks, count/localization and header layout assertions remain intact.

Validation: candidate diffs pass `git diff --no-index --check`. No Unity compile or Play rerun performed; no formal project files edited. The parent must compile/rerun after applying, and use the emitted gate values if another live hold remains.
