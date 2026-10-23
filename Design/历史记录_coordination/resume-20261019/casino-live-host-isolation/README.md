# R1 casino live-host review and minimal QA follow-up

Baseline includes parent's `IsolateTicks` TestMode=false change and the real-host wallet equality assertion. This candidate modifies only `Assets/LocalBuild/Editor/CasinoScamActualUiRunner.cs`; no production source or Claude C1/C2/C3 candidate was modified by this agent.

## Findings

1. **Keep TestMode=false.** `XingGuangHost.cs:26–41` explicitly maps TestMode=true to Money=0 and rejects Spend. False restores the real fixture household wallet and actual spending/earning, so it is required for the accepted final bet and exact settlement assertions. `SetSimulationForTests` sets TestMode=true on every bind (`ChapterOneRuntime.cs:366`), so the existing call to `IsolateTicks` after each bind and archive load must remain.

2. **Economy and ordinary disk writes remain isolated.** `ChapterOneRuntime.Update` only advances the household simulation under tickEnabled (`:149`); controller.enabled=false prevents the lab Update/clock/train loop. Runtime notifications still dispatch, which is needed for save-owner rebinding. `TryAutosave` and `SaveNow` still require useDiskSave (`:171`, `:196`). Runner enables disk only inside synchronous SaveSlot/LoadArchive calls with finally restoration. TestMode=false alone does not turn disk back on. Model components remain disabled and the existing no-model assertion remains intact.

3. **Live YY Update introduces unrelated message producers.** `YYChatHub.Update` (`YYChatHub.cs:707`) calls DeliverEra (`:755`), which now passes its TestMode guard, checks every two seconds when idle and waits twenty seconds after a delivered era line. `EraContent.TakeDue` consumes undelivered records from the fixture's June 2016 date without requiring the household clock to advance. This can violate the exact no-early/exact-one gossip assertions during the real 40-second wait. YY Update also queues AI-join work from the fixture's default locked contact state. `YYGirlfriend.Update` is independently active and now passes its TestMode guard. These are source-proven newly eligible paths, not a claim that a new Unity failure has been observed.

4. **Disable ambient hub updates while retaining the real receiver.** Hub Initialize subscribes to runtime Changed/Saving; OnDestroy removes these subscriptions. Its OnDisable only stops preset and cold-scene playback. Disabling the component does not remove persistence/rebinding events or prevent direct Receive/Select calls, and YYChatView is a separate enabled component. `CasinoDesktop.TickScamNotices` calls real hub.Receive and verifies exact owner, typing/intro state and count increment before consuming the receipt; it does not require hub.enabled. Therefore the isolated runner can keep the hub disabled throughout and still verify actual casino delivery, rendering, persistence and owner protection.

5. **Archive load and gossip timing remain real.** LoadArchive writes the restored snapshot to the isolated primary, calls Attach/Changed synchronously, and rebinds controller/hub even though their Updates are disabled (`ChapterOneRuntime.Slots.cs:58`, runtime Attach at `:117`). It does not perform an offline economy tick. CasinoDesktop detects the new XgSim owner on its next live Update, resets gossipPending, and starts a fresh 40-second schedule for an unconsumed beat. Existing immediate snapshot equality, loaded outcome/dedup checks, exact 40-second delivery, 41-second no-repeat wait and old-owner deadline assertions are unchanged. Existing explicit Today and Clock fixtures remain unchanged.

## Candidate and validation

The three-hunk patch adds `YYChatHub`, `YYGirlfriend`, and `AiJoinsYy` to existing QuietDrivers; removes Bind's hub re-enable and adds an assertion that it remains frozen; updates report Scope. Existing cancellation/disabling runs before initial fixture setup and after every bind/load. CasinoDesktop and YYChatView are left active. No timer injection, history seeding, assertion relaxation or additional test suite.

- Patch: `casino-live-host-isolation.patch`
- SHA256: `da0957f7307eed769dce53c827912c30651a475c0623d814f9a5a68111766731`
- Baseline SHA256: `d407b0c2fc04dbc100632901b701f0a2e53ae1cc5c53a7aefeb33739d23fc80a`
- Candidate SHA256: `3a0fde7ac53f0b92b0bacc5ac008a99cf7cb44e9bd375962ddf4475352354c27`
- `python .coordination/resume-20261019/casino-live-host-isolation/prepare.py` generated independent baseline/candidate/manifest and passed production `git apply --check` read-only. It also checked preservation of existing Need/Check lines, timing checks and snapshot equality.
- No C# compile, Unity run, player build or visual QA was performed for this follow-up. No runtime pass claimed.

Recommendation: integrate this QA-only follow-up with the parent's live-wallet fix, then run the same `CasinoScamActualUiRunner.Run` serially. Require both languages/all outcomes, freeze/closed disk roundtrips, active frozen blackjack, exact delayed group delivery/dedup and stale-owner checks to pass. Freeze C# edits during the run; the known hot-reload interruption limitation remains unchanged.
