# C5 arithmetic source provenance — staging only

No Unity, .NET or model execution. No production writes, commit or fetch. `git apply --check` is a patch-applicability check, not a compile/runtime pass.

Apply `arithmetic-provenance.patch` as minimal existing-file hunks, then copy the four files in `new-files.json` from `new/`. Never copy whole baseline/candidate files over root's concurrent changes. The Design baseline was refreshed after root's 06:15 correction; historical failure and preregistered numeric-relation results remain intact.

## Change

- Automatic mole whacks call `RecordAiAction`: `sourceKind=ai_action`, `answerSource=ai_action`, empty `playerAnswer`, no teaching note, no truth/answer supplied as supervision. Existing action settlement and score are untouched.
- Audited manual teaching callers keep `RecordTeachingQuestion`, which now writes `answerSource=player` on the linked note and log. This includes manual whacks and explicit review judgments. Recording itself still never trains; only the already-existing checked `TeachMoleStatement→TeachBoard→Board.Train` does.
- New answer provenance defaults to empty on old save records/notes. No migration guesses author from text, AI name, correctness, old sourceKind or game ID. All old text stays readable. Already persisted memory cards/votes are preserved.
- New human memory discovery requires known player provenance on both linked note and question log. `check_work`, `learned_wrong`, other note-derived memories, explicit corrections, gamble-answer evidence and new shutdown votes are guarded. Genuine new shutdown choice records explicitly stamp player. Books/retests/chat keep their separate existing receipt chains.
- Retest preserves original answer provenance and never becomes new player supervision. Actual new manual correction may follow unknown content but does not upgrade the original. Generic generated train-sheet records are `dataset_label`.
- Native YY notebook, files notes/CSV and ending source panels distinguish player, AI response and unknown history. CSV retains original answer columns but calls them recorded answers and adds Answer source. YY reask comparison (“matches what you taught”) requires player provenance. AI records can be revisited as question-only text; that is not teaching.

## Existing C5 implementation audit

The requested explicit calculus teaching loop already existed. See the staged Design table for the exact implementation/verification split. This package reuses it and fixes its record provenance instead of adding another loop. Numeric relations and all training/inference features are unchanged. Complex expressions, roots and calculus continue to use the symbolic fallback; no memorization/generalization claim is made.

An additional unpatched test gap is documented: the review buttons read the current reviewSlot when invoked, not a captured display token. Build-generation and owner guards exist, but cross-question navigation/held-pointer tests are absent. This package does not claim that scenario verified.

## Root-run verification

Core filters to include:

`LingGuangV05.XingGuang.Tests.XgArithmeticProvenanceTests` (12 generated test cases), `XgMoleTeachingTests`, `XgArithmeticInputTests`, `XgArithmeticRelationsTests`, `XgEndingPreparationTests`, `XgEndingNarrativeTests`, and learning-record/teaching-snapshot regressions.

The two existing ending test fixtures that explicitly represent human actions now stamp player; deliberately unknown-source cases are in the new tests. This fixture annotation is not production migration and does not relax date, ID/link, score, money or training assertions.

Actual UI entry points already exist:

1. `MoleReviewUiChecks.Run`, without `-quit` or `-nographics`. It independently selects Sidebar phase14 through BookLibrary's isolated scene/input lifecycle, timeout180s. It now additionally requires a real automatic whack (not just a leave decision), one AI action log per whack, no auto training/player note, plus linked player sources for manual whack/review. It retains the original direct LCD, reduced-effects, rebuilding/owner/hidden-surface checks. The previous inspected log failed at the empty-hole feedback assertion, so this is runnable QA, not a passing result.
2. Existing `SidebarLayoutChecks.RunLearning` for learning UI. Its ordinary bilingual 120-record pagination suite now ends with three explicit provenance fixtures (player/AI/unknown), actual LCD notebook opening/filtering and files selection. It captures YY, CSV and unknown note previews in both languages. No reask is submitted and no model response is fabricated. The provenance records are appended only after the original 120-record exact-count assertions.

The added UI assertions must run in root's serial Unity process. Screenshots require visual review; string checks alone do not prove text geometry or visual quality.

## Consumers audited

| Consumer | Handling |
| --- | --- |
| `XgActionMoles.Hit` | Splits manual vs auto writer before settlement; no false human note from auto |
| `RecordTeachingQuestion/RecordTeachingAsked` | New audited manual path stamps linked player provenance |
| `RepairLearningRecords/RepairLearningNotes` | No source backfill; old content kept |
| `RecordAsked` / train-sheet caller | Generated dataset label explicitly identified |
| `RecordLearningRetest`, retest input | Source preserved; visible question-only input, no training |
| `RecordLearningCorrection` | New explicit manual correction stamped; original unchanged |
| `DiscoverMemories`, `DiscoverAdditionalMemories`, `RecordShutdownAnswer` | Player-specific gates read provenance; old unknown cannot become new human prerequisite |
| `YYChatView.Learning`, `XgYyTalk.Learning` | Separate display and no AI/unknown “you taught” comparison |
| `XgFilesWindow.Records` | Answer-source CSV column and unknown-author note text |
| `XgEndingPreparationHost` | Source display does not assert unknown record was your answer/correction |
| `TeachBoard`, `RememberTaught/Correction`, `XgArithmeticInput`, `XgArithmeticRelations` | Unchanged; notebook log is not a training input, generated labels never enter inference |

`ChapterOneSim.SubmitCard(playerAnswer)` is an independent immediate player-card API, not a consumer of these saved learning records; it is unchanged.
