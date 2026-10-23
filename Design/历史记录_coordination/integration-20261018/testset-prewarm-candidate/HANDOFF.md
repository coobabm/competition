# Incremental test-set preparation candidate — review before merge

Files/pieces:
- testset-prewarm-candidate.patch: three NEW files (Core builder, Core per-sim pending coordinator, Desktop scheduler), one TestSet cache-miss expression in XgSim.Board.cs, one scheduler Attach line in XingGuangController.cs. Does NOT edit XgSim.cs or asmdef.
- testset-prewarm-checks.patch: isolated EditMode deterministic checks, TestSetPreparationChecks.Run.
- Both git apply --check passed. Nothing compiled/run; all production code remains untouched by this agent.

## Semantics
Private builder processes all4096 training indices in order, retains original400-card target and1600-candidate cap, same Seed/Make/Signature and spare ordering. No worker threads and no asynchronous shared static writes. Incremental signatures are intentionally NOT published to the shared TrainSignatures cache; this is more conservative than the initial plan and avoids changing cross-consumer cache lifetime. All temporary signatures/spares are released after completion; only complete card list remains.

At most one pending job per XgSim; changing target supersedes/releases prior private work. Result stays outside live testSets until a real cache MISS requests the exact dataset/level/FULL generation date/salt/Diagnostic purpose; culture name and sim English flag must also match. Existing same-month cache hit always retains original object. Mismatch or incomplete work is discarded and original synchronous XgBoardData.TestSet runs unchanged. Exam path is unmodified. Small-space fallback overlap is preserved, not silently eliminated.

Desktop LateUpdate only prepares when a live board sim is visible or a round is active and no exclusive stage presentation is held. Active training track has priority; otherwise Selected dataset. Up to32 generation/copy operations per frame, with a soft1ms stop checked after each operation, on the main thread. Single-card cold initialization/JIT can exceed1ms; this is not a hard scheduling guarantee. Owner swap/disable destroys pending references. Existing runtime Attach wires the component; no autonomous global GameObject or scenes.

## Limits to communicate
- This is opportunistic. A newly selected dataset, immediate first round, changed day/salt/level, or unsupported preparation window may still hit the original synchronous cold path.
- 4096+up to1600 candidates need many frames at32 units/frame. Cold synthetic tests starting a round immediately may intentionally not finish prewarming before the first assessment. Do not alter training duration/completion or reduce题量 just to make a benchmark pass.
- Board.Accuracy is never cached/precomputed here; actual trained board evaluation remains at original time.
- One active track gets priority, so simultaneous second track can remain cold. No general all-dataset background preload.
- This should be tested against the generator configuration/environment used by the application; dynamic modifications to static catalog contents are outside current runtime's normal behavior and do not have a version signal.

## Deterministic checks included (not executed)
Independent reference loops in test code; compare ordered card seed/truth/region/distance/text, ordered feature values/made flags, and full serialized source XgCard. Every catalog dataset at chunks1/7/32/4096; selected datasets at levels2/4, salts0/913, Diagnostic/Exam and new-month date. Assert4096 training iterations, <=1600 candidates and400 result count.

Checks pending invisibility, same-reference real consumption, initial date mismatch within month, retained cache after later same-month day, month rollover, cancellation during phases, sync arrival during partial preparation, salt/level invalidation, separate sim owner, unchanged serialized state after preparation, and5 actual training epochs vs non-prewarmed cloned state with equal host billing/money. Training fixture is new and unexecuted; fixture failures must be reported/repaired rather than bypassed.

Run compilation and TestSetPreparationChecks in a dedicated verification Editor; then actual Unity frame capture with Xg.TestSetIdlePreparation plus existing epoch markers. Compare memory and cold/warm first-evaluation behavior separately. A passing deterministic test is not gameplay/framerate acceptance.
