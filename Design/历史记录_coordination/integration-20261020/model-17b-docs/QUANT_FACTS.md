# Qwen3-1.7B-Q4_K_M — verified file facts / 随包文件事实

Companion to `QUANT_MODEL_CARD.md`. Everything here was read from the pinned upstream
repositories or from the GGUF header itself (a 16 MB HTTP range read of the first metadata
block; no weights were downloaded). Nothing in this file is copied from a blog post.

## Identity

| Field | Value |
|---|---|
| File | `Qwen3-1.7B-Q4_K_M.gguf` |
| Bytes | 1,107,409,472 (1.031 GiB) |
| Base model | https://huggingface.co/Qwen/Qwen3-1.7B |
| Base revision | `70d244cc86ccca08cf5af4e1e306ecf908b1ad5e` (main HEAD, "Create LICENSE", 2025-07-26) |
| Quantizer | Unsloth — https://huggingface.co/unsloth/Qwen3-1.7B-GGUF |
| Quant revision | `d7f544eead698dbd1f15126ef60b45a1e1933222` ("Update params", 2025-06-08) |
| License | Apache-2.0 (both base repo and quant repo declare `license: apache-2.0`) |
| Mirror | https://modelscope.cn/models/unsloth/Qwen3-1.7B-GGUF (HTTP 200) |

Base revision note: the base repo has 18 commits. The weight files date from the initial upload
(`c52c071034e3` 2025-04-27, `8d4744f9e130` 2025-04-28); every commit after the quantizer's run is
README, LICENSE or a single `tokenizer_config.json` edit (2025-05-19). No commit after 2025-04-29
touches `*.safetensors`. So pinning HEAD is safe for provenance — the weight bytes are the ones
that were quantized.

## GGUF header (read from the file at the pinned revision)

| Key | Value |
|---|---|
| `general.architecture` | `qwen3` |
| `general.name` | `Qwen3-1.7B` |
| `general.quantized_by` | `Unsloth` |
| `general.quantization_version` | 2 |
| `general.file_type` | 15 = `MOSTLY_Q4_K_M` |
| `qwen3.block_count` | 28 |
| `qwen3.embedding_length` | 2048 |
| `qwen3.feed_forward_length` | 6144 |
| `qwen3.attention.head_count` / `_kv` | 16 / 8 (GQA) |
| `qwen3.attention.key_length` / `value_length` | 128 / 128 |
| `qwen3.context_length` | **40960** |
| `qwen3.rope.freq_base` | 1000000.0 |
| Tensors | 310 |
| `tokenizer.ggml.tokens` | 151,936 |
| `tokenizer.ggml.eos_token_id` | 151645 |
| `tokenizer.ggml.padding_token_id` | 151654 |
| `tokenizer.ggml.add_bos_token` | 0 (false) |
| `quantize.imatrix.dataset` | `unsloth_calibration_Qwen3-1.7B.txt` (196 entries, 685 chunks) |

310 tensors = 28 layers × 11 tensors (attn_norm, wq, wk, wv, wo, q_norm, k_norm, ffn_norm,
ffn_gate, ffn_up, ffn_down) + `output_norm` + `token_embd`. A dense transformer with tied
embeddings and **no `nextn` / MTP block** — the "no MTP stripping" wording in
`MODEL_SOURCE.json` is literal, there is no MTP layer to strip.

## Quantization

- `Q4_K_M` is a llama.cpp **k-quant**: 4-bit weights with a 6-bit scale/min pair per super-block
  (nominal ≈4.5 bits per weight), and llama.cpp keeps token embeddings / the output head at higher
  precision. The file is 1,107,409,472 bytes for a 1.7B model ≈ 5.2 bit/weight overall —
  that is expected for a small model with a 151,936-token vocabulary, not a sign of a wrong file.
- This is an **imatrix / "dynamic"** quant (`quantize.imatrix.*` keys are present, calibration set
  `unsloth_calibration_Qwen3-1.7B.txt`), i.e. Unsloth's Dynamic 2.0 recipe, not a plain uniform Q4_K_M.
- No post-processing was done locally. The file is byte-identical to upstream.

## Context length: two numbers, neither is the game's

- Qwen model card: `Context Length: 32,768`.
- `config.json` `max_position_embeddings`: 40,960. GGUF `qwen3.context_length`: 40,960.

The GGUF wins at runtime (40,960). Irrelevant for the game: `LlmScheduling.SlotContext` is 4096 per
slot, so keep the docs saying "game slot context 4096" and do not advertise 32k/40k as a shipped
capability. `Assets/StreamingAssets/LLMManager.json` currently advertises 262144 for the 4B — see
`SYNC_LIST.md`.

## Chat template embedded in the GGUF

`tokenizer.chat_template` holds the **full Qwen3 Jinja template** (tool-call branch, `enable_thinking`,
`<think></think>` handling) — it is *not* the simplified 1,482-byte `template` file that also sits in
the Unsloth repo. Same ChatML `<|im_start|>` framing as the 4B, so the game's own closed-think trick
(`LlmScheduling.ClosedThinking`, appended in `WithThinking`) still applies unchanged.

## Sibling quantizations in the same pinned revision (bytes, from the HF tree API)

| File | Bytes | GiB |
|---|---:|---:|
| Qwen3-1.7B-Q2_K.gguf | 777,796,160 | 0.72 |
| Qwen3-1.7B-Q3_K_M.gguf | 939,539,008 | 0.88 |
| Qwen3-1.7B-IQ4_XS.gguf | 1,010,383,424 | 0.94 |
| Qwen3-1.7B-Q4_K_S.gguf | 1,060,190,784 | 0.99 |
| **Qwen3-1.7B-Q4_K_M.gguf** | **1,107,409,472** | **1.03** |
| Qwen3-1.7B-UD-Q4_K_XL.gguf | 1,132,952,128 | 1.06 |
| Qwen3-1.7B-Q5_K_M.gguf | 1,257,880,128 | 1.17 |
| Qwen3-1.7B-Q6_K.gguf | 1,417,755,200 | 1.32 |
| Qwen3-1.7B-Q8_0.gguf | 1,834,426,944 | 1.71 |
| Qwen3-1.7B-BF16.gguf | 3,447,349,568 | 3.21 |

Size change against the shipped 4B: 1,107,409,472 / 2,740,937,888 = 40.4 % of the old payload,
i.e. **1.63 GB smaller (−59.6 %)**. Everything else in the package is unchanged.
