# Independent life cadence/core/host review

Reviewed 2026-10-19. This agent performed source review and prepared a Main UI candidate. This report is not a Unity, native Windows, or disk persistence PASS. Parent owns serial Unity execution. Production sources were read only by this agent; reviewed source hashes are in `reviewed-files.json`.

## Findings and disposition

1. **Fixed: ordinary long frames froze active cadence.** The first host candidate gated time by `frameSeconds <= Time.maximumDeltaTime`. The project has `runInBackground: 1`; a slow active frame does not prove minimization. Latest host uses native minimized/explicit pause state and one observed recovery frame only. The original simulation dt/economy were not changed.
2. **Fixed: due stage conflict was missed outside Sim.Tick.** Controller skips Tick while stage presentation holds, so detecting conflict only inside TickLifeEvents could miss the required 30-second post-conflict grace. Latest core exposes clock-neutral `ObserveLifeEventConflict`; Controller calls it before the stage skip and in the save-slot early return. Core has a no-Tick stage-conflict regression.
3. **Fixed: first real opening timer depended on finishing opening focus.** Buying `train.curve` after 12 answers was possible while `openingFocus` remained set; the original unlock gate prevented the first eight-minute countdown. Latest core removed only this focus gate and retains actual presentation/ending checks. Core's standalone report is `passed`, 127 checks, `unityExecuted: false`; it includes BeginOpening → 12 answers → real BuyNode.

No additional blocking correctness defect found in the reviewed final core/host source. The integration boundaries below remain explicit.

## Data integrity and cadence review

- One-time cadence migration resets the obsolete random timer/mode while preserving nonzero independent RNG, permanent seen/history/flags, and repayment obligations.
- Same mode does not reset the interval; selecting a different mode starts the selected full interval. Off retains unresolved choice/result state.
- Successful choice applies consequences once, clears open choice, commits bilingual result, and records history. A repeated answer rejects once the open choice is gone.
- Result view reads saved text; localization does not reroll or reapply consequences. First acknowledgement clears result and starts the selected full interval. Repeated acknowledgement does not reset it.
- Card saves after the completed core mutation, using the existing house+lab save transaction. Runtime runs all Saving callbacks before a single atomic payload write; a callback exception prevents partial persistence. Wallet Spend/Earn mark dirty but do not save midway through choice application.
- Card captures both lab and household binding. Its click callbacks reject a changed controller sim or runtime household before the next card Update, protecting cross-save callbacks.
- Repayment uses its independent life clock, removes a due repayment before crediting, and persists its removal. It is separate from event cadence.
- Eligible unseen events have priority. Cycling clears only currently eligible cycle IDs after that eligible pool is exhausted; permanent seen, flags, consequences and history survive.
- Stage/reader/group conflicts suspend cadence, and due conflicts receive the saved 30-second grace. Ordinary focus loss or internal AI window visibility does not gate active time.

## Group scripts and lifecycle

The current `YYChatHub.GroupPresets` generation/owner guard has the required ordering: invalidate generation before stopping old coroutine; retain unconsumed queue on disable; resume only same owner; dequeue only immediately before Receive; old finally does not clear newer work. Swapping saves changes owner identity and immediately makes old playback ineligible for the host hold, even before an old wait finishes. YY BindSave also clears script handler/choices.

Full Assets searches found no `group_intro` loader or state machine. The only non-null YY Script registrations are old Zhou private conversations in PrologueDirector and ZhouCardLoan. XgWiringRelay contains clearing calls only, not an active group registration. `Design/ui/v4_世界支线源码对照.md` W05 explicitly records the six group_intro scripts as not yet implemented. There is therefore no present permanent hold caused by a registered group_intro handler in the reviewed production code. When that future player is added, it must expose actual playback state, rather than retain a group handler while idle. Current predicate covers the active preset coroutine and a group-bound handler; it cannot prove absent authored flows.

## Boundaries still requiring integration evidence

- Actual standalone Windows minimization/restoration and Alt-Tab/fullscreen focus behavior require native player verification. The handle probe validates own PID/UnityWndClass and IsIconic; Editor injection does not exercise the OS.
- No actual separate foreground book reader exists in reviewed production. Its owner must register visible state with `SetReadingWindowVisible` and unregister on hide/disable. Destroyed owners are pruned. Background AI reading is correctly excluded.
- No central player pause implementation was found. Save slots already skip Controller; a future actual pause owner must call `SetGamePaused`. Transient hit-stop is intentionally not used as a pause proxy.
- Prepared group lifecycle checks have not been run by this reviewer; live authored dialogue delivery is additional integration coverage.
- Main result rendering, actual atomic disk persistence and card rebind are covered by the prepared UI candidate below, but must not be reported as passing until parent executes it.

## Main UI candidate

`candidate/Assets/LocalBuild/Editor/LifeResultUiPlayQa.cs`, entry `LifeResultUiPlayQa.Run`.

Checks actual Main card choice → saved result → acknowledgement, 5000 loan principal charged once, one history/repayment, duplicate answer rejection, checksum-valid primary disk payload, JSON disk rebind without repeated wallet/history/repayment/RNG mutations, Chinese/English committed text, rendered glyph bounds and missing substitutions, actual canvas raycast target plus pointer click handler, stale confirm/choice callbacks across a save rebind, full 480 seconds on confirmation, duplicate confirmation not resetting time. It emits choice/result LCD PNGs and a JSON assertion report.

No Unity was started or compiler invoked by this agent. The fixture freezes runtime ticking and the controller, so it validates acknowledgement state exactly; scheduler time progression is covered by core tests rather than a real eight-minute wall-clock wait. It performs a JSON/disk reload in one process, not a full process restart. It uses pointer-handler injection, not physical mouse automation.
