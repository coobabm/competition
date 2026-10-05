# Laya evaluation and gated game integration

> Execution: native in this session, following executing-plans and test-driven-development.

**Goal:** Evaluate the real multilingual Laya checkpoint on the game's generated logic questions, and integrate only if it passes a declared acceptance gate.

**Architecture:** Link the existing pure C# XgLogic generator into a small JSONL exporter. Python uses the official Laya package locally, passes only question text to inference, and writes raw predictions plus aggregate metrics. Game mechanics, player checkpoint simulation and saves remain unchanged unless the gate passes.

**Tech Stack:** .NET 8, Python 3.12 isolated venv, laya 0.3.24, PyTorch; existing Unity 6000.4.5f1 / Inference Engine 2.6.1 for the conditional second stage.

**Spec:** User request in this conversation: local evaluation first, then real Laya advice alongside simulated player model if acceptable, then native packaged inference.

## Constraints and review focus
- Work in the live `/Users/kk/competition` checkout; preserve all pre-existing dirty/untracked work. No scene edits, global package changes, paid APIs or remote writes.
- Default gate fixed before inference: Chinese accuracy >=75% in both suites and >=60% in each of nine balanced-suite categories; no missing, failed or truncated predictions. No threshold tuning on this test set.
- Balanced suite: 20 distinct questions per category, 10 yes/10 no. Natural suite: 100 deterministic draws per level, duplicates retained and disclosed to reflect the existing generator distribution.
- Reuse the game's generator rather than porting it. Pass no ground truth, reason, seed or category to the model.
- Raw P(yes) and max(P(yes), 1-P(yes)) are not calibrated real-world correctness. Report Brier/ECE, class baselines, high-confidence errors and latency separately.
- Pin model revision and verify weight/tokenizer hashes. Measure batch-size-one wall latency including tokenization; record warmup separately.
- Exceptions and truncation must fail closed, never silently disappear from denominator. Reports must say if incomplete.
- Fixed second prompt on balanced suite is a diagnostic, not a post-hoc replacement for the primary gate.
- Number-sequence answers follow the generator's intended simple rule, not a mathematically unique continuation. No claim of template-disjoint generalization.

## Tasks
1. [x] Write tests for JSONL schema, deterministic export, per-category balance/uniqueness, probability validation, metrics and fail-closed gate. Run RED.
2. [x] Add `Tools/XingGuang/laya/Export.csproj`, `Export.cs`, `evaluate.py`; run the complete tool tests and existing XingGuang tests GREEN.
3. [x] Pin dependency lock; download `convaiinnovations/laya-multilingual` revision `e4e9ddf21a7b1903b7acffd8814ad4307bf63a67`. Export 680 rows; run Chinese and English primary plus balanced diagnostic. Preserve per-row outputs, source hashes, hardware/version metadata, report.
4. [x] Inspect acceptance gate. If failed: write measured rejection report, do not activate game advice or package weak model; retain reproducible tooling for later fine-tuning. This is a conditional stop, not completion of all three requested stages.
5. [ ] If passed: use native tokenizer first and test Python/C# parity; export floating ONNX, test Unity operator compatibility, then Unity-supported quantization with size/error measurements. Wire asynchronous advice with cancellation/stale-card guards; no game truth used as fallback. Verify Play mode and a local target Player build before claiming stages 2/3 complete.
6. [x] Independent final Python review, whole tool/game regression, document remaining limitations and exact artifacts.

## Execution ledger
- Baseline: active Editor project path confirmed; existing XingGuang .NET suite 27/27 passed.
- Ruling: no speculative tokenizer or UI implementation before model passes. Existing Inference Engine contains tokenizer support; its ONNX importer lists DynamicQuantizeLinear/MatMulInteger as unsupported. Official ONNX Runtime INT8 export is not a drop-in Unity asset.
- Dependencies installed in tool-local venv, no global environment changed.

- RED observed: absent exporter/evaluator; then GREEN 8 tests. Review regression RED observed for malformed all-yes/duplicate balanced corpus; after validation fix GREEN 10 tests.
- Two complete 1,720-inference offline CPU runs: Chinese balanced 88/180, natural233/500; no errors/truncation. All probabilities identical between runs.
- Gate rejected; task5 NOT STARTED. User stages2/3 remain unimplemented, not silently marked complete.
- Independent review confirmed real corpus quotas, exact job set, all metrics and hashes. Specialist reviewer backend unavailable; equivalent fresh-context reviewer completed the read-only audit.
- Final artifacts: Documentation/Laya/REPORT.md, questions JSONL, both raw runs; tool README and dependency lock. Core generator hash remains unchanged.
