# Ending narrative visibility candidate

Only two production-target files: `XgEndingPreparationHost.Narrative.cs` and `XgEndingPreparationHost.cs`. Apply `ending-narrative-visibility.patch` from the project root, or copy the two candidate files after verifying baseline hashes in `manifest.json`. No production file was edited while preparing this package. No core memory/vote/save semantics, ending actual QA, or shared runner changes.

## Findings and fixes

- Original PollNarrative checked Current and a 0.5-second timer but no external presentation gate. Original ShowSeed/OpenGallery likewise allowed opens during story or exclusive presentation. The candidate reads StoryFocus.Active (including Exclusive), StagePresentationHeld, InnerVoice, reading/pause, actual native Settings.isOn, SaveSlotsPanel.IsOpen, Epilogue.HoldingSimulation/Showing, committed release, and chapter completion. The visible preparation/trial/night area also suppresses this optional UI. Show(area) immediately suspends it.
- The gate runs before the polling timer, in LateUpdate before rendering, and in every new narrative button callback. Suspended panels are deactivated before deferred Unity destruction. Answers are not consumed and cooldowns are not extended on suspension: NextShutdownPrompt can return the pending question when ordinary play resumes. Gallery opening closes the question; an open Gallery prevents automatic questions. Manual questions remain possible outside testing while core NextShutdownPrompt still offers them; only automatic opens require testing.
- No self-Holding registration is added. Source inspection: XgStageCurtain.Install assigns ExclusiveProbe to stage Busy or XgEpilogueDirector.IsHolding. StoryDesktopPresenter.StoryFocusNow reads StoryOnScreen, including cutsceneRoutine (the month presentation), narration, ExternalBusy, and its settle interval. Neither probe includes XgEndingPreparationHost. Its own area simply suppresses optional narrative surfaces, without altering StoryFocus or area visibility.
- Old Later, Gallery Back/Previous/Next/card/launcher callbacks now require the same current save/state, captured live panel identity, active hierarchy, and external gate. Closing/replacing a panel clears its field before destroying it. Switch-save reset invalidates every captured old panel.
- Source Back captures its own source view and parent instead of destroying whatever sourceView happens to be current. Back remains usable after an unrelated presentation revision change but requires the same save/state and live panel. Multi-source record Back returns to its source selector; selector Back returns to Gallery. If revision changed, navigation refreshes Gallery. Closing/replacing Gallery cleans its source view and sourceShown immediately. Preparation source records use their explicit area parent, so an unrelated Gallery cannot steal them.
- Poll caches only prompt, memory count, and phase by combined flow/epilogue revision plus stage. Unchanged polls do not call GetSnapshot or NextShutdownPrompt. Save replacement clears the cache. Gallery and mutations still read live data. Settings uses existing DesktopWindowIndex and a cached WindowManager, not repeated scene scans.

## Verification status

Static `git apply --check` passed against the recorded production baseline. Candidate files and patch are UTF-8 LF. No Unity run, C# compilation, runtime assertions, profiler result, or screenshots are claimed.

Root-owned Unity follow-up (not an added shared runner):

1. With a real eligible pending question, show it; trigger stage/month curtain, story, native Settings, save panel, and ending area individually. It must deactivate before rendering; retaining/invoking an old option or Later callback during the hold must not mutate save state. Resume ordinary play and verify the same question returns. Answer it normally and verify the next eligible prompt and normal cooldown.
2. Open Gallery, then a multi-source card. Open a source, Back to selector, select another, Back twice. Confirm only one source view and sourceShown false when closed. Repeat with a background presentation revision change; Back must still close and refresh instead of trapping the player.
3. Retain old Gallery/card/source/Back callbacks, close/reopen the same surface, then invoke the old callbacks. The new surface must remain untouched. Repeat across a real save switch.
4. Open the actual stage-6 preparation/trial/night area. No launcher, question, or Gallery may obstruct its controls. Closing it restores ordinary automatic narrative eligibility; closing a Gallery by itself must not permanently lock questions.
5. Profile unchanged 0.5-second polls: no repeated full ending snapshots. A successful answer, memory discovery, route change, stage transition, or save replacement must refresh the cached values on the next allowed poll.

Broader gaps retained: C7 independent risk forecast consumer and authoritative disclosed anniversary date remain incomplete. This patch does not change them.
