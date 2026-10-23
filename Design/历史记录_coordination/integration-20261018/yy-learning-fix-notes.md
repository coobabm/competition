# YY learning repairs

Actual disconnected paths fixed:
- Notebook/persona questions used SendAuthoredChoice (record-only). They now call normal hub.Send, with ready conversation and owner checks, so Heard and delayed Deliver run.
- teachingNotebook and training sheets had no RecordAsked calls. Actual teaching entries and actual SheetAfterRound questions now populate askedLog (existing per-bank limit retained).
- ChatNodeReady was never consulted by tech Status/Buy. Unowned tech now gates on the completed dialogue; owned legacy tech stays owned. Hint and saved actual reply are visible in tech details.
- Exact player text is stored separately from trimmed display/model text. New field defaults empty for old saves. Unlock matching uses it after successful delivery, ignores byAi messages as before.
- Design ID attention_only maps explicitly to existing transformer node; none of the 12 requirements is dropped.
- Stage-six continues quiz is triggered ONLY after a real notebook reply is delivered AND starts with 下一题：/下一题:/接着出题：/接着出题:/Next question:/Next problem: AND contains a question mark. No stage-only or ordinary follow-up trigger. This is a deliberately narrow evidence rule; other forms of continuation remain unrecognized rather than invented.

30 actual .NET core assertions pass (yy-learning-fix-check/test-results.txt): all12 node IDs valid, actual purchase blocked then allowed, question records, exact phrasing, successful delivery, two tags, no duplicates, continues marker positives/negatives/stage/once. Full Unity Roslyn+WorldYyLearningChecks static compile zero errors. No Unity launched by subagent.

WorldYyLearningChecks.Run uses isolated inactive GameObject/TestMode, restores static YY pointers, drives real AskFromNotebook/AskPersona -> hub.Send -> Heard -> Deliver and tagging API. Existing silence behavior gives a deterministic response without model requests. The continuation case injects a controlled reply only inside QA then invokes actual delivery; production never forces that reply. It is not a screenshot/button-layout test. Parent must run it in Unity.

Potential migrations: tests purchasing bias/mlp/etc without their dialogue now need to explicitly deliver the dialogue or state that they test already-owned legacy tech; do not remove the new gate to satisfy obsolete tests.
