# Qwen replacement — active 2026-10-03

User explicitly replaced the Laya task: use the already configured Qwen3-4B-Q4_K_M.gguf only. Existing model size: 2,497,280,256 bytes. Do not download another model, delete old assets, or continue Laya diagnostics.

At this heartbeat the new instruction was sent to Claude Cowork / game / Ripple游戏模型升级, app://localhost/cowork/local_6325352a-daf2-42ee-b4a9-c6303c2fe5ff. UI confirmed a new You said bubble and Stop response; do not resend the initial instruction. Claude is implementing the replacement; this chat will inspect and run actual Unity validation after the reply. Avoid concurrent edits to Talk/MainRoot/judge files until that work is inspected.

Baseline: .coordination/integration-current.md reports an earlier 124-test and Windows smoke pass with both CPU Laya and Qwen. That is historical evidence, NOT validation of this new replacement. The other integration chat is idle. MainRoot currently still creates LayaService and wires cards, exams, and expression selection to it.

Requested boundaries: share Talk's existing model backend and serialize the native slot; independent judgment context, no persona/history or expression-level gate leakage; bounded parsed choices; honest cancellation, timeout and invalid-output behavior. Qwen self-reported confidence is not calibrated probability; update labels and automation decisions accordingly. Math remains programmatic. Remove Main/build dependencies on Laya while retaining original files.

Pending: inspect Claude changes, verify new assemblies are compiled/loaded, run focused parser/routing/lifecycle tests and real Qwen requests including a choice and yes/no, verify cancellation and chat isolation where supported, report actual results to the same Claude conversation. Pause automation only after replacement validation and handoff.
