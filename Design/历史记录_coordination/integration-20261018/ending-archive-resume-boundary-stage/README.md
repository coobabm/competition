# Ending archive branch-resume boundary candidate

One editor-runner file only; no product code edited. No Unity/Player, compilation, model/audio or C# tests were run. Original `EndingArchiveActual/20261018-231243-360` remains failed and untouched.

## Evidence

- The prior branch persistence stage passed all four strict predicates. Its protected before/after per-file hash ledgers are identical.
- The saved branch `2d1bd44d064449659bac986b57f90f99` still holds the exact integer marker `1094554`; its primary and backup have that amount and correct branch identity.
- The completed primary and its backup differ only in `lastSeenUnix`, consistent with the already-declared fresh-title initialization before the final read-only observation. Completion lab JSON is unchanged. All other recorded protected files still match their post-first-branch hashes.
- Original source and EvidenceCopy still match `3219a8f641deb8ce0a91f16af34dcb0c8b88fd3de471ce267ab7457a5d3b8478`.
- The failed final check did not persist individual live values, so the exact post-resume amount or reason for its inequality is not claimed proven from the historical report. Failure evidence inventory and read-only comparisons are in `failure-audit.json`.

## Why change the QA boundary

`CaseMenuHud.ShowEndingBranches` calls `ResumeEndingBranch` synchronously, then closes the archive and invokes `archiveStart`. `ResumeEndingBranch` switches the store and calls `Attach`, which publishes `Changed` synchronously. The actual `BeginGame`/`EnterGame` sequence subsequently calls `EnterDesktop`, which re-enables gameplay hosts. Existing `TitleTap` and `Until` yield real frames before the final check. Comparing live money to a load marker at that late boundary can conflate correct restoration with subsequent real earnings.

The candidate now:

1. Reads the branch's actual disk payload before the real row click; exact marker, branch ID and checkpoint must match.
2. Temporarily observes runtime `Changed`. The observer only copies values; it does not assert, invoke a save, stop a host or intercept input. It captures the first new simulation, store path, branch/checkpoint, exact money and total earned, and verifies later that this notification preceded gameplay/transition start. Event cleanup is in `finally`, including failure/disposal paths.
3. After the real click, requires exact restored marker and identity at that captured binding boundary. The assertion retains exact equality and is not replaced by a tolerance.
4. Lets the real desktop flight/startup run, then records live money and total-earned values/deltas separately. It continues requiring the same restored simulation, correct store/branch/checkpoint, and byte-identical original primary, manual slots and immutable checkpoint trees. Existing end-of-measurement QA isolation remains after the transition; no earlier freeze is introduced.

Diagnostic report fields are persisted before each new assertion. No claim is made that income itself was validated, that the historical failure became passed, or that a product defect was fixed. The serial owner must apply and run the actual UI suite to establish the new binding and transition results.

`audit.py` reads only historical evidence and writes its result here. `package.py` performs static delimiter/baseline-drift checks and produces `ending-archive-resume-boundary.patch`. No new tests are added; the existing actual-UI runner's observation boundary and diagnostics are updated.
