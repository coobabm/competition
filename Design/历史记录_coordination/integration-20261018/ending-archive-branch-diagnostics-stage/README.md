# Ending archive branch persistence diagnostic candidate

Scope: one editor-only runner file. Production files have not been edited. No Unity, Player, C# build or test has been run by this audit.

The historical `EndingArchiveActual/20261018-225153-642` result remains failed. Its four-part branch persistence assertion did not record the expected money value or individual predicate results. This candidate does not claim that the historical failure was proven to be floating-point precision, or that a product defect was fixed.

Read-only evidence:

- Original source tree and immutable EvidenceCopy both still match the saved source tree SHA256 `3219a8f641deb8ce0a91f16af34dcb0c8b88fd3de471ce267ab7457a5d3b8478`.
- All five original manual-slot files and the original chapter snapshot match EvidenceCopy byte-for-byte.
- New completed primary contains receipt `epilogue:31f5b053f7a44d5f949f35fd8b5a5473`. Its mtime is 22:53:02 UTC, before branch creation at 22:53:32 and branch marker save at 22:53:40. The checkpoints' mtimes precede branch creation too. There was no persisted pre-branch primary/checkpoint ledger, so timestamps alone are not treated as definitive proof.
- Branch identity `022c76614e164e1180c83b63f209ec55` and destination are independent. Its final money is `1094556.6773014963`. The real Main transition resumed hosts and produced earnings and narrative state before the test froze them. That behavior is preserved by this candidate.

Candidate changes:

- The explicitly declared branch-only money fixture uses `Math.Floor(money) + 7` and requires a finite, exactly representable integer below 2^53. This preserves exact equality and removes incidental fractional serialization precision from a store-isolation check.
- Before/after money values are recorded as invariant round-trip strings plus IEEE double bits; pre-save, post-save and disk values are distinct.
- Before/after primary SHA256, slot tree digest, checkpoint tree digest and per-file protected-root ledger are saved to report.json before the original assertion. Branch directory is excluded from the diagnostic protected-root ledger because it is expected to change.
- All four original predicates remain strict. Primary still uses byte SequenceEqual; slots and immutable checkpoints still use complete tree digests. No tolerance, omitted file, product income change, rewritten old evidence or replacement passed report.

`audit.py` only reads historical evidence and writes `failure-audit.json` here. `package.py` performs static delimiter and baseline-drift checks. The serial owner must import and rerun actual UI; old failure remains failed even if a later run passes. R2 manifest/bibliography work is deferred behind this actual failure.
