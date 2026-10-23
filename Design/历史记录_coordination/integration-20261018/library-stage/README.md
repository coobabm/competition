# C4 book library candidate — 2026-10-19

Candidate only. No production file was edited, no Unity instance was launched, and no commit/push was made by this candidate agent. The parent owns runtime/browser/independent reader UI, real JsonUtility/disk-save validation, and integration.

## Delivered behavior

- Stage 2 `books.library` technology entry costs ordinary ¥500. It bypasses StagePrice and the old 0.25 multiplier; existing legitimate technology discounts still apply. Unlocking grants no books.
- Exactly 100 stable entries: 90 Chinese originals and 10 English originals. Purchase bands are ¥50–150 short/introductory, ¥200–500 classic, ¥800–1500 long/advanced. Each purchase has its own stage/date/money gates and one committed debit.
- The eight old IDs, chapter text, parameter rewards, existing read progress and active reading bars are preserved. Legacy saves receive their eight implicit entitlements. New null-state games start with no owned books; the actual `XingGuangController.BindSave` was checked and passes null when labState is empty. Migration does not invent old completion dates or history entries.
- The other 92 entries provide two distinct newly authored bilingual guide paragraphs and a comprehension question. These are not full texts, translations, publisher notes, or covers. Their character counts reflect the actual short supplied guide, and they grant no new parameter bonuses. The reader must show the material label rather than presenting them as complete purchased novels.
- Completion commits progress, queue, stable C3 record and legacy story beat before external chapter/finished callbacks. Stage <4 sends an honest guide/excerpt reaction through existing YY chat. Stage >=4 uses existing book-puzzle/YY mechanics; duplicate asks/answers cannot replay rewards.

## Integration

`book-library.patch` is the complete candidate. `shared.patch` contains the five shared hooks (catalog, price, technology copy, constructor initialization and completion-card copy); `library-core.patch` and `checks.patch` separate the remainder. Apply the complete patch OR the split patches, not both. `manifest.json` gives candidate and C3 dependency hashes; `static-verification.json` records the latest `git apply --check` against the current production working tree.

C3 prerequisite: the parent-integrated `XgSim.LearningRecords.cs`, `XgSim.YyLearning.cs`, and `XgSim.DataPacks.cs`. Only the C3 API `RecordBookCompletion(bookId,title,titleEn,completionKey)` is called. `dependencies/` contains frozen compilation inputs, not another patch to overwrite the parent's implementation. This candidate changes no persistence host or storage limit.

Public integration points:

- `XgBooks.Library` / `LibraryEntry(id)` provide bibliography, price, stage, conservative availability, original language, source URL, and material rights.
- `sim.LibraryOpen`, `OwnedBooks()`, `BookOwned(id)`, `BookPrice(id)`, `BuyBookBlocker(id,host)`, `BuyBook(id,host)` support storefront and shelf. Use the actual host for debits. `BookPurchased` is raised after ownership/purchase state is committed.
- `XgBooks.MaterialLabel(book,english)` and `Excerpt(chapter,english)` distinguish supplied guide vs existing public-domain excerpts. The existing old eight Chinese excerpt bodies stay Chinese in English UI; they have no supplied modern translated full text.
- Reuse `StartReading`, `ReadingBook`, `ReadingProgress`, existing chapter and YY answer paths. New guides do not add a separate executable answer UI.
- `PendingBookCompletion`, `BookCompletionText(record)`, `AcknowledgeBookCompletion(key)` expose a durable one-shot notice queue. Choose this OR an immediate `BookFinished` presentation, avoiding two notices for one event. Save after mutation/acknowledgement through the parent's normal safe runtime path. Attach/detach callbacks by Sim/runtime identity and reject stale callbacks after reload.
- Update the existing inline training shelf: its old `XgBooks.All` loop would now show all 100; the independent shelf should use `OwnedBooks`. Route/remove the old inline quiz UI in the parent's reader/YY work.

## Evidence and rights

`catalog.tsv` and `guides.tsv` are editable source inputs; `generate_catalog.py` generates the data class and `catalog.json`. `sources-and-rights.csv` has all 100 entries and explicitly separates bibliography sources from actual in-game material. `migration-map.csv` has all eight unchanged IDs/rewards.

`source-audit.json` records actual successful HTTP fetches for all 100 bibliography links and short year/context excerpts; `legacy-text-source-audit.json` records successful fetches for eight original-language Wikisource works. These checks prove retrieval and provide review traces, not scholarly certification of every historical date. Ancient dating and disputed authorship are qualified; `availableBy` is a conservative sale threshold, not a fictional precise first-publication date. Four Generations Under One Roof uses the pre-2016 1983 Chinese restoration, not a later 2018 retranslation. All entries are available by the game's 2016 period.

Wikipedia audit excerpts are attributed to Wikipedia contributors at the individual article URLs under CC BY-SA 4.0 (https://creativecommons.org/licenses/by-sa/4.0/). Audit snippets are not shipped reading prose. New guide paragraphs and questions were authored for this candidate; no article/news prose, copyrighted book text, modern translation, jacket image, or publisher commentary is included. The eight legacy original-language excerpt datasets were retained, not expanded with new borrowed text. This is a content/provenance assessment, not a jurisdiction-wide legal opinion.

## Verification and remaining parent work

`dotnet run --project Checks.csproj --verbosity quiet`: **2103 new assertions +23 adapted legacy tests, 0 failures**. The harness compiles actual Core source with frozen C3 dependencies, using System.Text.Json only as a test round-trip adapter. `core-result.json` holds results. Existing warnings remain in unrelated DayStats/event declarations and the legacy fixture.

Coverage includes 100/90/10 counts and unique IDs, 92 unique guide pairs, price bands/source/material checks, ¥500 unlock/legitimate discount/no free ownership, real commerce failure/success/duplicate behavior, migration/idempotence/in-flight preservation, all 92 real reading transactions, one-time history and durable completion acknowledgement across reload, all 92 existing YY question/answer paths, no invented cross-book theme, English guide-note labels, and no main-line dependence on library completion.

Parent must run `BookLibraryChecks.Run` / menu `Tools/Local Verification/Book library purchase and reading` using actual Unity JsonUtility, then the normal Unity tests and runtime UI/save/reload checks. Browser purchase, independent shelf, reading overlay, YY focus and reply routing, notice presentation, window rebind, and real save slots remain integration work and have not been claimed as complete here.
