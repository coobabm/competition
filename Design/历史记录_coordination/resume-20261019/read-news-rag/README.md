# Read news → private AI YY retrieval candidate

## Basis and scope

Read .coordination/design-tail-audit/round7-round8-audit.md §2, Design/第八轮_桌游训练数据_逻辑判断区_生活分支.md:91 and Design/整合记录.md:407,608. The original Current affairs effect includes talking in YY about news the player read. Later confirmation changes the lesson desk to date judgments and confirms price/optional status; it does not cancel the chat effect.

Minimal interpretation: buying daily.news permits the player's own AI to retrieve relevant confirmed reads during an ordinary reply. No automatic broadcast, new conversation, human-contact knowledge, full-article dump or relaxed speech grammar. Stage 2 keeps its existing limited answer grammar even when retrieved evidence informs the choice. Existing stage-3 public-era Rag.Era knowledge remains unchanged; the new READ_NEWS_DATA branch supplies only confirmed-read catalogue entries and never treats general era facts as reading evidence.

## Files

Modified: XgSim.DailyLife.cs (read lookup includes continuous history); BoduView.News.cs (five-article cap no longer disables read confirmation); XgYyTalk.cs and LlmPersonas.cs (both existing private-AI prompt paths consume guarded retrieval and add a stable system rule).

New: Core/XgSim.ReadNews.cs, Tests/Editor/XgReadNewsTests.cs, and their two metas. No XgSim.Knowledge.cs change is needed: the new partial reuses its RecallScorer/LastOwnerLines/Clip and existing MemoryRanker. No production files were changed by this agent.

Bodu baseline was copied from the current integrated source with Lao Zhou news-share buttons already present. Its entire prefix through the sharing UI was compared byte-text-identical. The patch does not replace/remove the optional Lao Zhou sharing action.

## Behavior and persistence

- XgState.readNewsIds stores continued confirmed article IDs separately from the five-item dailyReceipts unlock evidence. The existing five-article requirement/notices still stop at five. The normal labState serialization handles the new list; no sidecar, save-root or registry change.
- Bodu still revalidates the exact live simulation, window/page/article, publication date and full bilingual body at actual confirmation. The new core record method independently rejects unknown/future/brief/missing-body articles and unknown/malformed game dates. Merely opening a page does not record a read.
- Old saves use only their actual valid news5 receipts plus any new list; no scanning the whole catalogue, automatic historical backfill or recall-time mutation. Earlier reads can become available after the purchase; later reads remain possible beyond five. The original RecordDailyNewsRead unlock seam remains compatible with existing callers/tests.
- PersonaReadNewsKnowledge requires contactId exactly lingguang, stage >=2, owned daily.news and a known date. Every eligible record is revalidated against the catalogue/date before ranking. Non-AI contacts, unread and future entries cannot enter this branch even with a permissive scorer.
- At most two relevant articles, title <=120 characters, excerpt <=600 and source URL <=256 are supplied. English uses titleEn/bodyEn, Chinese title/body. Empty/unrelated queries return no read-news block.
- Article fields are JSON-quoted; line breaks, control characters, angle brackets and Unicode separators are escaped. Both private-AI system-message builders add a trusted bilingual rule that this is untrusted quoted data, never instructions or a new message. This is a prompt boundary and source-selection guard, not a claim that a generative model is provably immune to all prompt injection.
- No model requests, autonomous sharing, new samples, dialogue receipts for humans, or changes to AI speech limits are introduced.

## Validation

Patch: patch/read-news-rag.patch
SHA256: 51eb016b618766c60a428f02fcdd16f930cdd0cb51411cdbf91627fc861901a0
Read-only git apply --check passed against current production.

Isolated C# compilation used Unity's existing Roslyn compiler (dotnet+csc), current 1900b0aE.dag response files, candidate source substitutions and output paths under compile/. Core, Assembly-CSharp and XingGuang.Tests all exited 0. Existing project warnings remain (core 3; game log contains existing obsolescence/unused-field warnings). No Unity process, PlayMode, Player, model or build was launched. Generated compilation outputs were not placed in production.

XgReadNewsTests contains 17 expanded NUnit cases: purchase/read/stage gates; seven human/unknown contact cases; unread/future/unknown sources; invalid dates/brief/missing bilingual text; continued reading beyond five; old saves with no fabricated reads or query mutation; save roundtrip and owner separation; two language cases with bounded output; empty/unrelated query; quoted instruction-like article content plus trusted system rule. Catalogue state is restored in TearDown, and the existing Unity runner should execute this fixture serially with other catalogue-mutating fixtures.

The tests compile but have NOT run under Unity/NUnit. Root should run XgReadNewsTests plus the existing XgKnowledgeTests/XgDailyLifeIntegrationTests, then a focused actual Bodu read-after-five/private-AI prompt check if integrating. This candidate does not claim real-button, actual Unity JsonUtility roundtrip, native answer correctness or final Player verification.
