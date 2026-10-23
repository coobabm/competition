# E-B integration handoff — 2026-10-19

The staged native-platform patch is ready for the root integrator. No Unity, compiler, .NET command or model was run by this agent. Do not replace shared hosts wholesale from `candidate/`: use the focused patch so unrelated changes remain intact.

## Apply

From `competition-latest-20261016`:

```powershell
git apply --check ../.coordination/integration-20261018/ending-discourse-stage/platforms.patch
git apply ../.coordination/integration-20261018/ending-discourse-stage/platforms.patch
```

Ordinary `git apply --check` passed before handoff. `package.py` writes the patch as UTF-8 bytes to preserve LF context. `manifest.json` covers 17 changed/new paths; hashes normalize text newlines. `base/` captures the host baselines and `candidate/` contains the proposed files. Regenerating the package changes no production files. `prepare_resource.py`, in contrast, writes the production JSON and should not be rerun while root is checking Unity.

## Already in production

- `Assets/LingGuangV05/XingGuang/Core/XgPublicDiscourseCatalog.cs`: parser, public-only context and content visibility gates.
- `Assets/LingGuangV05/XingGuang/Core/XgPublicDiscourseSchedule.cs`: pure delivery timing and receipt state; host persistence is still required.
- `Assets/LingGuangV05/XingGuang/Tests/Editor/XgPublicDiscourseTests.cs`: six authored source tests. The former generic test-folder placement was corrected with its .meta preserved.
- `Assets/LingGuangV05/Resources/LingGuangV05/Epilogue/ending_public_discourse.json`: 30 stage cards, 15 route cards, three shared headline/comments, ten sources; bilingual text extracted from the finalized source/dialogue design. The former Story location was removed, preserving .meta.
- `Design/终局并行实施契约_20261019.md`: public-event receipt semantics and the explicit Epilogue resource-directory override.

## Patch contents

- Desktop projection adapter scoped to the actual `ChapterOneSim` and persisted revision; callbacks reject a changed owner/revision.
- Real Bodu archive page, platform-origin tabs, detail text and source citations. `BoduView.OpenPublicDiscourse("news", "H-01")` is the shared epilogue headline entry.
- Real Tieba home/list/detail archive, Miaoyu public-discussion tab and YY netbar-group archive. The YY archive never creates messages, unread counts or membership.
- Explicit browser-excerpt catalog query. The installed browser can show labelled public excerpts from other platforms without pretending those native apps are installed or that a private message arrived.
- Eight resource/integration-contract source tests: content grid, bilingual coverage, date gates, public-event gates, browser/native distinction, local-only privacy, legacy-news deduplication and cross-stage notification timing limits.

Stage W cards describe somebody else's public example site. R4-W uses the approved D5-W fallback and is labelled accordingly. R4-M excludes the private-decision narrator. Historical sources remain separate from fictional people/events. The Transformer headline is unavailable before 2017-06-13; its cited preprint is explicitly 2017-06-12.

## Required shared integration

1. E-A/root must publish the saved projection after successful commit/load using `XgPublicDiscourseDesktop.SetReleaseProjection(actualChapterOneSim, savedRevision, displayDate, route, successfulArtifactId, savedPublicEventIds)`. Use the saved display date without advancing the game economy.
2. `PUBLIC_EVENTS.md` lists all exact event IDs. Ten R1/R2 cards require actual authored public source entities and corresponding successful event receipts. Publication alone does not imply use, public sharing, correction replies, derivatives or listings. E-A currently provides receipt plumbing and publication only. E-B does not grant event IDs; absent receipts legitimately hide those ten cards.
3. Persist `XgPublicDiscourseDeliveryState` in the save and drive the pure scheduler from safe foreground runtime. Fresh games initialize with `newGame: true`; migrated saves use `false` to avoid backlog alerts. After the host actually accepts a notification, call `RecordDelivered`; held/failed delivery must leave no receipt. Respect the host's dialog/voice/important-event busy state, and route accepted notification clicks to the real relevant surface.
4. Root must perform serialized Unity compilation, the 14 new test cases, and visible interaction checks for Bodu/Tieba/Miaoyu/YY in both languages, including a changed save while a page is open. No runtime, rendering or click-validation success is claimed here.

The active notification policy in the core is 60 safe seconds in the current stage, 180 seconds between any prompts, 300 seconds per platform, at most two per rolling 600 seconds, 30 safe seconds after important events and one notification per stage. Existing AlphaGo and 2016-09-28 topic records retain legacy IDs and cannot send duplicate new alerts.
