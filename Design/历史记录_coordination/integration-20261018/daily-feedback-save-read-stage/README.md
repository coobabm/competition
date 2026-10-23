# Daily final save: read technology unlocks

One Editor QA file. XgSim.Has reads S.unlocked for these three non-persona technologies; Buy writes S.unlocked. S.owned is dataset ownership. The old aggregate assertion incorrectly looked for technology IDs in owned.

Read-only inspection of the failed 20261018-233219-846 primary verified its payload SHA256: all three technology IDs are present in unlocked, owned is empty, sequence epoch=4, household money=1000828.1475503766. Historical report remains failed. Its successful minimize/hidden/reopen/reduced checks may be cited individually, not as an overall pass.

The candidate deserializes the same actual AtomicSaveStore primary payload, logs each technology's disk unlocked/live Has/dataset owned predicates and disk/live epoch plus round-trip money, then asserts each purchase separately, exact disk/live epoch equality, at least four clicked natural rounds and finite nonnegative wallet. No production purchase, serialization, storage or evidence changes. Real purchase price checks remain unchanged. No save rewriting, new test suite or synthesized completion.

Static delimiter and baseline checks plus git apply --check only. No Unity/Player/compilation/tests/model/audio run. Serial owner must rerun to confirm the corrected final assertion path and clean teardown.
