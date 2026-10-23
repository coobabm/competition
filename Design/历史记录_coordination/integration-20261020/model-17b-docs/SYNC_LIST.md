# Files that still name Qwen3.5-4B — sync list / 待同步清单

Produced 2026-10-20 against `competition-integration-20261020` @ `defe2035`. Nothing in the repo was
modified. Line numbers are from that commit; the working tree had only an unrelated dirty design doc.

Priority: **P0** = wrong in a shipped artifact or makes the build/QA fail · **P1** = documentation
that would become false · **P2** = historical record, leave alone.

## P0 — must change for the swap

| # | File:line | Now | Change to |
|---|---|---|---|
| 1 | `Assets/LingGuangV05/LocalLLM/LocalModelRelease.cs:6-9` | `Name = "Qwen3.5-4B"`, `FileName = "Qwen3.5-4B-Q4_K_M.gguf"`, `Bytes = 2740937888L`, `Sha256 = "00fe7986…"` | `"Qwen3-1.7B"`, `"Qwen3-1.7B-Q4_K_M.gguf"`, `1107409472L`, real hash. **You said you'd do the constants** — flagged here because `Bytes`/`Sha256` must match `MODEL_SOURCE.json` or the release check trips. |
| 2 | `Assets/StreamingAssets/LLMManager.json:6-7` | `"filename"`/`"path": "LingGuang/Models/Qwen3.5-4B-Q4_K_M.gguf"` | `LingGuang/Models/Qwen3-1.7B-Q4_K_M.gguf`. Editor/Play-mode load path; without it Play mode silently fails to start the model. |
| 3 | `Assets/StreamingAssets/LLMManager.json:8` | `"embeddingLength": 2560` | `2048` (Qwen3-1.7B hidden size). Stale metadata for the 4B's hidden dim. |
| 4 | `Assets/StreamingAssets/LLMManager.json:10` | `"contextLength": 262144` | `4096` — matches `LlmScheduling.SlotContext`, which is what the game actually allocates. 262144 was the 4B's metadata max and never the game allocation. Note `IntegratedReleaseBuild.cs:45` rebuilds this entry at build time, so these two fields only affect editor/Play runs — set them honestly anyway. |
| 5 | `Assets/StreamingAssets/LingGuang/Models/Qwen3.5-4B-Q4_K_M.gguf.meta` | Unity meta for the old file (`guid: 9fe804df055059d40a1d51acc694cbfe`) | Delete together with the old `.gguf`, let Unity mint a fresh `.meta` for the new file. Leaving it orphaned gives you a dangling GUID; copying it over gives the new model the old identity. Neither is what you want. |

## P1 — would become false

| # | File:line | Now | Change to |
|---|---|---|---|
| 6 | `Assets/LingGuangV05/README.md:89` | "2026.10.19.5 的本地聊天模型为 Qwen3.5-4B（Q4_K_M），已完成文件校验及隔离场景 CPU AVX2 真实推理验证…此前 Windows 包使用 Qwen3-4B，其验证结果不能证明新模型已通过。" | New model name + `Qwen3-1.7B`（Q4_K_M，1,107,409,472 字节 / 约 1.03 GiB）, and the 此前 clause becomes `Qwen3.5-4B`. **Do not keep the "已完成…验证" half-sentence until the 1.7B has actually been run** — the whole point of that sentence is that old evidence doesn't transfer, and that applies to this swap too. |
| 7 | `Assets/LingGuangV05/README.md:92` | "目标模型：…Qwen3.5-4B-Q4_K_M.gguf，来自固定版本的 unsloth/Qwen3.5-4B-GGUF，原始文件 2,740,937,888 字节。" | `Qwen3-1.7B-Q4_K_M.gguf`, `unsloth/Qwen3-1.7B-GGUF`, `1,107,409,472 字节`. |
| 8 | `Assets/LingGuangV05/README.md:101` | "与此次 Qwen3.5-4B 聊天模型不同" | `Qwen3-1.7B`. (The embedding-model caveat itself still holds: no embedding GGUF ships, retrieval is BM25.) |
| 9 | `Assets/LingGuangV05/XingGuang/README.md:141` | `## 本地大模型（Qwen3.5-4B）` | `## 本地大模型（Qwen3-1.7B）` |
| 10 | `Assets/LingGuangV05/XingGuang/README.md:143` | "模型为 Qwen3.5-4B（Q4_K_M，原始文件 2,740,937,888 字节，约 2.74 GB）。完整文件校验、加载与隔离场景真实回复已通过…此前 Windows Qwen3-4B 证据不适用于新模型。" | New name, `1,107,409,472 字节，约 1.03 GiB`, 此前 → `Qwen3.5-4B`. Same warning as #6 about not inheriting the "已通过" claim. |
| 11 | `Assets/LingGuangV05/XingGuang/README.md:146` | "**目标模型位置**：`…/Qwen3.5-4B-Q4_K_M.gguf`。来源为固定版本的 `unsloth/Qwen3.5-4B-GGUF`" | Both names to 1.7B. |
| 12 | `Assets/LingGuangV05/LocalLLM/LocalLlm.cs:31` | "Local language model for free chat (Qwen3.5-4B GGUF by default)." | `(Qwen3-1.7B GGUF by default)`. Comment only. |
| 13 | `Assets/LocalBuild/Editor/SerialNativeQa.cs:99` | Assertion message `"Pinned Qwen3.5 release model is actually running"` | `"Pinned Qwen3-1.7B release model is actually running"`. The check itself uses `LocalModelRelease.FileName`, so it keeps passing either way — only the log text is stale. |
| 14 | `Assets/LingGuangV05/Core/Chat/LlmPromptLayout.cs:8-13` | "The local model (Qwen3.5) is hybrid recurrent: the server can only resume from checkpoints it saves at the start of the last user message…" | **See the architecture note below.** The layout decision stays correct; its stated reason does not. |
| 15 | `Assets/LingGuangV05/Core/Chat/LlmScheduling.cs:29-33` | "Qwen3.5 is a hybrid (recurrent + attention) model: a prompt can only be resumed from a saved checkpoint…" | Same problem. Comment must change; `CheckpointsPerSlot = 6, CheckpointSpacing = 64` need a decision, not a blind rename. |
| 16 | `Documentation/LingGuang-Incremental/素材使用声明-填写稿.md:32` | "Qwen3.5-4B（Apache-2.0，bartowski Q4_K_M）、Qwen3-Embedding-0.6B…" | `Qwen3-1.7B（Apache-2.0，Unsloth Q4_K_M）`. Note **`bartowski` was already wrong** in this line for the 4B payload — the shipped 4B came from Unsloth. Fix the quantizer while you're there. |
| 17 | `Documentation/LingGuang-Incremental/素材使用声明-填写稿.md:46` | `Qwen3.5-4B / Qwen3-Embedding` | `Qwen3-1.7B / Qwen3-Embedding`. |
| 18 | `Documentation/LingGuang-Incremental/开发引用.md:53` | "Qwen3.5-4B（阿里通义千问，Apache-2.0；bartowski 量化 Q4_K_M，约 2.9 GB）" | `Qwen3-1.7B（阿里通义千问，Apache-2.0；Unsloth 量化 Q4_K_M，约 1.03 GiB）`. Same pre-existing `bartowski` error; also 2.9 GB ≠ 2.74 GB. |

## P2 — historical, do not edit

- `RELEASE_NOTES_2026.10.19.5.md:3,21,29-31` — a released note describing what that build shipped.
  Rewriting it would falsify the record. Write new release notes for the 1.7B build and link back.
- `Documentation/LingGuang-Incremental/策划案v1.1落地计划.md:961,1035,1072,1090` — the 4B sample-output
  evidence and the "hybrid recurrent" rationale. Keep as evidence of what was measured **on the 4B**;
  add a dated line saying it no longer applies. Two things must not be recycled: the sample replies
  (#961, #1035) are not 1.7B output, and #1090's `--cache-reuse` claim is architecture-specific.
- `Design/交接_上游整合暂停_20261020.md`, `Design/整合记录.md`, `Design/老周知识问答…_2026-10-19.md`,
  `Documentation/Devlogs/*`, `Documentation/Releases/*`, other `Documentation/LingGuang-Incremental/*`
  — dated records. Leave.
- `Build/MyComputerAlive-2026.10.20.1-…/**`, `LocalVerification/Release/latest-build.json` — generated.
  `latest-build.json:10-12` carries `"model": "Qwen3.5-4B"`, `modelSha256`, `modelBytes`; it must be
  regenerated by the build/QA pass, never hand-edited.
- `Tools/LocalLLM/strip_qwen_mtp.py` — tool for stripping Qwen3.5's MTP block. Qwen3-1.7B has no MTP
  layer (see `QUANT_FACTS.md`: 310 tensors, dense), so it is inert for this payload. Keep for the
  history of the 4B repackaging; do not run it on the new file.
- `Tools/LlamaSharpProbe/Program.cs:10,13` — feasibility probe with a hard-coded `/Users/kk/...` path.
  Already broken on this machine; rename the comment if you like, no functional impact.
- `Main-source/Assets/LingGuangV05/LocalLLM/LocalLlm.cs:21` — a stale mirror of an older implementation
  (it still starts `llama-server` as a process; the live file runs LLMUnity in-process). Decide
  separately whether `Main-source/` is still maintained at all; syncing one comment into a dead copy
  adds noise.

## Architecture note (the one that is not just a rename)

`LlmPromptLayout.cs` and `LlmScheduling.cs` describe, and partly justify, the prompt-caching design
with a property of Qwen3.5: it is a **hybrid recurrent + attention** model (Gated DeltaNet blocks,
per the 4B model card), so llama.cpp can only resume from checkpoints taken at the start of the last
user message.

**Qwen3-1.7B is not that.** It is a plain dense transformer — 28 blocks, full attention, 16 Q / 8 KV
heads, no recurrent state, no MTP. Prefix KV reuse works normally; `--cache-reuse` is no longer
ignored; the checkpoint mechanism is not the constraint any more.

Practical consequences:

1. The **comment blocks** at `LlmPromptLayout.cs:8-13` and `LlmScheduling.cs:29-33` are now false and
   are the kind of thing a reviewer will quote back at you. Rewrite them around "prefix caching works
   on a dense model; keeping the system message and few-shots byte-identical still makes each turn
   cheaper".
2. The **layout itself** (`LlmPromptLayout.AddState` riding in the last user turn) is still correct
   and still helps — it is now a plain prefix-cache optimisation rather than a workaround. No
   restructuring needed.
3. **`CheckpointsPerSlot = 6` / `CheckpointSpacing = 64`** exist to force a resumable checkpoint per
   turn. With a dense model they are no longer required; they cost RAM per slot
   (`CacheRamMiB = 2048` is the parking budget). Do not delete them blind — measure first, then decide.
4. Everything else in `LocalLlm`/`LlmScheduling` (ChatML framing, `ClosedThinking` suppression,
   `</think>` stripping, slot routing) is architecture-neutral and carries over unchanged. The GGUF's
   embedded template is the full Qwen3 Jinja template, same `<|im_start|>` format.

## Verified: nothing else names the model

- No user-visible string in `Lang.En.cs` or the UI names the model; the disclosure/status text is
  generic ("本地模型在线", "正在唤醒本地模型…"). No localization change needed.
- `Rag.cs`, `LocalEmbedder.cs`, `MemoryRetrieval.cs`, `LlmPersonas.cs` mention `Qwen` only in
  connection with the **embedding** model (Qwen3-Embedding-0.6B) or as a fictional brand name inside
  persona prompts ("千问都是没听过的名字"). Those must not be touched.
- `Assets/LocalBuild/Editor/IntegratedReleaseBuild.cs:59` says "canonical Qwen GGUF" — generic, fine.
