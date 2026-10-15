# Correction write capture — installed and frozen

2026-10-13. Parent-authorized scope: XgBoard.cs, XgSim.Board.cs, XgSim.AnswerTrace.cs, XgSim.cs, AnswerTraceChecks.cs only. Applied once after parent confirmed Unity run 54 exited; every source matched its expected SHA256 first. No Unity launched here.

The public two-argument Train API is unchanged. An internal optional evidence list captures actual R1 parameter writes after pinned/Dropout guards and captures actual R2 creation before R4 decay. No post-training state inference remains. Capture lists are created only for subscribed correction answers; regular training allocates no evidence collections.

Before/After describe each R1 write or R2 creation, not final net values after decay. A new R1 cell starts at 0/1 after initialization; R2 creation uses an absent 0/0 baseline. Created is set only from actual successful creation.

Evidence: `LocalVerification/DesignPass/CorrectionWriteCompile/runner-result.log`. Production core compilation passed with only the pre-existing DayStats CS1718 warning. Genuine core execution passed existing answer/prediction parity and immutability checks, clamped + decay counterexample, saturated training-counter positive control, actual R2 combinations, and existing Dropout counterexample. Only harness logging and in-memory JSON cloning use standalone .NET adapters; Unity integration and visual acceptance remain the parent's work.

`hashes.json` records original and installed SHA256 for all five files. `changes.patch` is the exact applied delta. Preparation/install scripts are historical reproducibility aids; the guarded prepare/install scripts intentionally refuse to overwrite the now-changed production baselines.
