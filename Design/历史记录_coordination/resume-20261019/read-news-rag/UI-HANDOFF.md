# ReadNews actual UI helper — paused handoff

User requested completion of the current candidate and then pause for Claude handoff. This file records prepared work only; no Unity execution or actual UI PASS is claimed.

## Ready candidate

`ui-candidate/Assets/LocalBuild/Editor/ReadNewsRagActualUiChecks.cs` and its new `.meta` are complete. `patch/read-news-rag-ui.patch` only adds those two files. It does not edit production, SidebarLayoutChecks, LaoZhouNewsCommentsUiChecks or the earlier feature patch. Read-only apply-check passed against production.

The feature dependency remains `patch/read-news-rag.patch` (SHA256 51eb016b618766c60a428f02fcdd16f930cdd0cb51411cdbf91627fc861901a0). Root has reviewed it; check actual production state before applying, do not reapply if integrated. This helper requires its new fields/methods and prompt wiring.

The helper follows the existing news-comments suite's real browser/headline navigation, explicit scroll placement and FilesWindowUiChecks.Click. It adds LcdActualUiProbe waits before ordinary targets. Every nested helper is explicitly foreach-flattened; Sidebar's MoveNext-only stepping does not skip inner waits/actions.

## Written assertions (NOT executed)

Both Chinese and English:

- Open and confirm six actual published full articles through real LCD buttons: lz2_t01_alphago, lz2_t02_xiaoice, n0601, lz2_t03_tay, lz2_t04_resnet, n0607. Opening alone does not mark read; after the sixth, history is six while unlock count is five.
- Deliver the sixth confirmation twice in the same frame through LCD input; only one record is added. Fourteen counted read clicks across the two locales include those two duplicates.
- Use the actual Back-to-headlines button, then the still-rendered old read button in the same frame. The current-page guard must reject it.
- Replace the save owner explicitly via SetSimulationForTests, then click the old rendered read control through LCD before redraw. Neither the old six-read owner nor the new empty owner may be mutated. This save replacement is a declared fixture, not an actual Load UI click.
- Restore the earned snapshot, call real isolated SaveSlot(5)/LoadArchive(5), and reopen the sixth article through actual UI. Six IDs and the five-item unlock count must survive; the sixth article shows Finished reading with no repeat action. Slot/primary writes are guarded to a dedicated LocalVerification path only.
- Construct production LlmPersonas.Conversation prompts with synthetic gaokao/高考 queries. Before purchase, no read payload; after the real core BuyNode call, sixth-article localized data appears only for lingguang, never in Lao Zhou/netbar/Qingwen/oldfriend prompts. The unconfirmed seventh article is absent. The dedicated XgYyTalk stable system builder is checked for the trusted source rule. This does not invoke AskNow or a model.
- Compare human forum state and every non-private-AI YY conversation before/after reads and prompt construction. No human message or fact may appear automatically. Core purchase is declared setup, not a tech-tree UI verification.

The fixture explicitly sets stage 2, date 2016-06-30, installed apps and sufficient funds. Independent story/economy/notifier producers, controller/YY ambient loops and forum auto-producers are frozen with enabled-state restoration. Already-running AI-join/card-loan scenes fail preconditions. Native browser, actual focus, LCD relay and input restrictions remain active; no force-focus probe or click-handler invocation was introduced.

## Root/Claude integration steps when work resumes

1. Confirm the feature patch is present, inspect this helper and apply only its two new files. Do not overwrite root's shared Sidebar file from any older candidate.
2. Root owns a new optional Sidebar entry/phase and an IEnumerator field. After existing isolated Main preparation and GameText.Bind, construct `ReadNewsRagActualUiChecks.Frames(ctl, languageCallback, captureCallback)` using the same language/capture callbacks as RunNewsComments. Set report.scope to its Scope. A 420-second bound matches the current comparable suites.
3. On each frame call MoveNext; when done collect AssertionCount, ReadClicks and Checks, dispose the iterator, then use the existing Finish/Restore lifecycle. Finish must dispose the active iterator on failure/interruption as well, so its finally restores original simulation, language, flags and drivers. Do not set StoryFocus.Probe or bypass foreground gates.
4. The runner must supply isolated save/profile paths, disabled runtime ticks/disk by default, inactive chat/embedding models, and real application foreground. The helper refuses otherwise. Its slot-5 writes occur only inside the guarded isolated directory.
5. Run only after a fresh compile when work resumes. Inspect the four expected screenshots (sixth-read and sixth-reloaded in each locale), report/log, save evidence and constructed-prompt assertions. Retain failures without converting fixture/prompt assertions into natural-play or model-output claims.

## Compilation and remaining work

Final helper C# compilation exited 0, with four Unity FindObjectsSortMode obsolescence warnings. It used Unity's existing Roslyn dotnet+csc with the already-compiled feature Core/Assembly-CSharp candidate references and current Editor helper assembly. All outputs are under compile/; see ReadNewsRagActualUiChecks.rsp and read-news-ui.log. No Unity, Player, model or build process was started.

NOT DONE: production integration, shared Sidebar phase wiring, Unity import/compile, core-test execution, actual UI run, disk-save runtime proof, screenshot inspection, model inference or final Player acceptance. No extra work is running. No new task or automatic wakeup was created.

## EndingPrivate read-only follow-up

Reviewed the current root version containing TapNativeClose, plus EndingPrivateDisclosure.Offers, XgEpilogueFlow eligibility, XgEpilogueMailBridge, EndingLetterPresenter and EndingLetterUiChecks.ScrollToEnd. No further definite incorrect precondition was established by this read-only pass. The route/ask ordering explicitly returns a single first offer before separate answer/shutdown offers; local-only fixture release requires no launch-night receipt; the 20-line bilingual answer stays below the dynamic 4096-character limit; presenter names SaveArticle/OpenLetter/ReadFirst/BackToDesktop/Paragraph_* match source, and its mail bridge grants completion eligibility to the non-readonly local fixture used by ScrollToEnd.

This is NOT proof the suite passes. Actual native-window layout, focus, long-preview wheel targeting, mailbox restoration and cleanup still need root's resumed run. No private-ending production or QA patch was made in this follow-up.
