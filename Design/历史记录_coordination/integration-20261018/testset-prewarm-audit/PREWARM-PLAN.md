# Test-set first-use stall: evidence and semantics-preserving prewarm plan

## Evidence and diagnostic patch
Capture 20261018-123757-252-8acf22 / raw-analysis-20261018-124117-833, train frame125:
- StepEpochs251.596191ms.
- EpochEvaluatePhenomena213.391296ms.
- ObservePhenomena201.927505ms (94.63% of grouped evaluation), RecordTrace5.623500ms, WatchCure5.812800ms, Evaluate0.012200ms.
This is consistent with first-use TestSet construction inside ObservePhenomena and subsequent cache reuse by Trace, but not yet proof of exact allocation/construction cost.

`testset-accuracy-event-markers.patch` adds only bridge scopes using existing XgSimulationMarker: Sim.TestSetLookupBuild, BoardData.TestSetTotal, BoardData.TrainSignatures, BoardData.TrainSignaturesMiss4096, Board.Accuracy, Sim.PhenomenonMessageAndSubscribers. No Unity dependency in Core. No logic or loop limits changed. Apply-check passed; no compile/run performed.

## Exact current contract to preserve
1. TrainSignatures iterates all indices0..4095, Make(dataset,Seed(dataset,index,Use.Train,salt),level,today), then Signature, then HashSet.Add. There can be fewer than4096 UNIQUE signatures; 4096 generation operations are mandatory.
2. Test candidates retain use (Diagnostic or Exam), index0 onward in exact order. Stop when accepted count400 or index1600 reached. Reject signatures present in training set into `spare` in encounter order, capped400; accept other cards immediately. Finally append spare in encounter order only until400.
3. Existing small-space fallback can include training-overlapping signatures. Do NOT claim perfect disjointness for those datasets; preserve that explicitly documented fallback. Do not add deduplication, reorder cards, change index advancement, alter Seed, truth/features/source, or silently replace fallback.
4. Test construction is independent of the live trained board: generation must not call Board.Train, consume S.rng/run.cursor, bill money, advance epochs, emit events, or change training results. Board.Accuracy is NOT precomputed, as board/knobs may change before evaluation.
5. Static signature cache key uses dataset|level|FULL today|salt. Sim test-set cache key uses dataset|level|MONTH|salt (non-topical date0), while a miss generates with full Today. Consequently earlier-day prepublication into the monthly Sim cache changes first-use semantics, even within the same month.

## Proposed implementation, after timings confirm
A. Pure Core incremental builder, not a background Task. Capture immutable dataset/level/exactGenerationToday/salt/use; keep `trainIndex`, `candidateIndex`, `spareIndex`, phase, private signature set, accepted list and spare list. `Advance(maxCards)` executes a bounded number of the SAME Make/Signature operations in original order. Use a single implementation shared by sync and incremental paths only AFTER reference equivalence checks pass; otherwise retain sync reference during development.

B. Main-thread owner service advances at most one builder with a soft ~1ms deadline per rendered frame and a hard small-card quota (e.g.32, tuned after actual measurement). Check deadline after each card; a single Make call is indivisible, so no hard frame guarantee. The service owns scheduling; Core has no Unity/time API. Never yield while holding trainSignatures lock, never enumerate or mutate shared static generator structures from a worker thread. Use the same language/catalog/generator configuration as normal calls; invalidate if a relevant mutable input changes.

C. Completed train signatures can enter the existing shared signature cache only atomically under the existing lock, after all4096 iterations; if another synchronous caller already filled that exact key, prefer the existing complete set. Never publish a partially built HashSet. Test list remains in a private pending store, NOT the live `testSets` dictionary.

D. At the real Sim.TestSet request: first respect any existing monthly cached set exactly as today. On a cache MISS, derive the exact full generation tuple at that instant; consume a completed pending set only if dataset/level/exactToday/salt/use match. Then install it into the original monthly key. A pending result generated earlier in the same month but a different day is not valid for this first-use request. If there is no matching finished result, use the current synchronous path unchanged; correctness takes precedence over avoiding all stalls. Cancel stale pending work/results on sim replacement, salt/level/dataset/date changes, scene lifecycle teardown, or generator configuration invalidation. Do not clear live monthly sets merely because day changed.

E. Queue only likely soon-needed Diagnostic sets for the current selected/training datasets while the player is teaching or the training bar is running. Do not start all datasets globally and create a memory spike. Keep at most one active builder and a tiny bounded completed queue (current/next track); cancellation releases partial lists and signature buffers. Exam requests retain their independent use seed and current timing unless explicitly scheduled under a separate exact tuple. No delaying epoch completion, score publication or event dispatch to wait for prewarm.

## Tests before production switch
- Keep a reference implementation of original loops in TEST code; compare accepted cards in order via seed/truth/region/features/text/source fields, not just accuracy, across every dataset, representative levels, multiple salts, topical days and months, Diagnostic/Exam, and small-card-space fallback.
- Verify all4096 indices visited and original candidate early-stop/spare behavior. Run incremental chunk sizes1/7/32/4096 to prove chunk boundaries do not alter results.
- Ensure no partial cache visible and cancellation at every phase leaves subsequent sync TestSet result equal to reference.
- Exercise old/new simulation owners, same-month day rollover BEFORE first use, rollover AFTER an existing monthly cache entry, month change, level/salt change, two pending requests for same tuple, and sync request arriving mid-job.
- Compare deterministic training snapshot/state with and without prewarm (RNG, board, runs, money, events, assessment, traces). No extra labels or learned samples.
- Measure cold first-evaluation construction on a fresh process/save, warm repeats, frame distribution while prewarming, and peak managed memory. Do not use only mean FPS or promise all first-use stalls disappear; an unprepared new dataset may still fall back synchronously.

This is a design proposal only. No prewarm algorithm or cache semantics have been changed in production/staging yet; only diagnostic markers are supplied.
