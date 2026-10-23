# Qwen3.5-4B compatibility audit — 2026-10-19

Read-only source audit of `competition-latest-20261016`. No Unity/native process started, model downloaded, or production source edited. This report is the only write.

## Minimal change set

1. Install the actual Qwen3.5-4B Q4_K_M GGUF under `Assets/StreamingAssets/LingGuang/Models/`, then move the old Qwen3-4B GGUF and its .meta outside active StreamingAssets. `LocalLlm.cs:448-459` sorts every *.gguf ordinal and picks the first >100 MB except mmproj; it does not read LLMManager. The old Qwen3-4B sorts before the historically used `Qwen_Qwen3.5-4B-Q4_K_M.gguf`, so merely adding the new model silently keeps loading the old model.
2. Update `Assets/StreamingAssets/LLMManager.json` filename/path and `Assets/LocalBuild/Editor/IntegratedReleaseBuild.cs:35` canonical modelName to exactly that file. The build redirects matching manager entries and enforces exactly one GGUF at the canonical location. Check shared-editor manager entries for stale old-model includeInBuild selections: the current helper only redirects entries whose basename already matches the new canonical name.
3. Replace model card, provenance/SHA256SUMS and applicable model attribution with the actual downloaded source and *final deployed* hash. An MTP-stripped copy has a different hash from its download. Update `LocalLlm.cs:31` default model comment and current records. Keep historical QA records historical; their old-model success does not certify new model.
4. Derive manager metadata such as contextLength from the actual GGUF; current 40960 is Qwen3 metadata. Gameplay context is independently fixed at 4096 per slot in LlmScheduling, so manager context does not change it.

## Loader, runtime and chat behavior

- Manifest pins LLMUnity commit `9931a9ea46906b1f43adaf9498c05ccf227b5d0b`; the corresponding cached package.json says 3.0.3. Deployed native directory is LlamaLib-v2.0.5. Preserve this known runtime first and verify the new model before considering upgrades.
- Windows deliberately starts CPU mode (`LocalLlm.cs:218-222`); LlamaLib chooses the CPU DLL. The historical successful run used llamalib_win-x64_avx2.dll.
- `Core/Chat/LlmScheduling.cs:29-34,97-120` already includes Qwen3.5 hybrid/recurrent checkpoint policy, 4096 context per slot, q8_0 KV, `--ctx-checkpoints 6`, and `--jinja --reasoning-budget 0`. Existing prompt layout targets hybrid cache reuse. There is no necessary broad scheduler or prompt rewrite for the model switch; do not apply pending Qwen3-only comment/cache proposals as part of it.
- `CreateNative` calls `EnableReasoning(false)` before service.Start. `Complete` uses the GGUF's template through `LLM_Apply_Template` under TemplateLock, then normalizes a trailing empty think block to `<think>\n\n</think>\n\n`; thinking=true replaces it with `<think>\n`. `AfterThinking` removes text before the final closing think tag. These mechanisms already accommodate the original Qwen3.5 flow. Preserve TemplateLock: the code records LlamaLib 2.0.5 apply-template crashes under parallel access.
- No production C# prompt injects `/no_think` or `/think`. The old Qwen3 model card mentions these soft switches; replacing that card is necessary. Do not add `/no_think` as the Qwen3.5 mechanism: use the existing template/reasoning setting and inspect the actual rendered suffix. If a new quantizer's template does not emit the expected trailing block, WithThinking leaves it unchanged, so test both nonthinking chat and stage-6 thinking if claiming both work.

## MTP stripping hazards

`Tools/LocalLLM/strip_qwen_mtp.py` documents the exact old-runtime failure `missing tensor 'blk.32.ssm_conv1d.weight'` for newer Qwen3.5 MTP exports. It drops the final `nextn_predict_layers` blocks, removes that metadata key, and decreases block_count, retaining remaining tensor data. This is appropriate only after validating that the downloaded export has this layout.

- It unconditionally reads `<arch>.nextn_predict_layers`: absent key raises KeyError. An already stripped/non-MTP file should be used as-is, not run through this script blindly.
- It assumes block_count includes the trailing MTP layers and computes keep_blocks = blocks - mtp. Verify actual tensor names, block count and nextn metadata before transformation; metadata convention must match this assumption.
- It has no input/output-same-path guard, overwrite guard, or post-write verification. Write a new staged destination, never overwrite the source or active model, then read back metadata/tensor count and hash it before promotion.
- It rewrites GGUF metadata/offsets; the docstring's “metadata byte for byte” is too strong. Values except intended edits are preserved, not necessarily serialized bytes. Preserve source URL/hash and transformed hash separately.
- `LocalLlm.cs:290-291` currently diagnoses every missing blk tensor as MTP. That warning is too broad to prove MTP; a corrupt/incompatible GGUF can also miss tensors.

## Existing real-inference verification entrypoint

Use the existing isolated batch Editor runner `-executeMethod SerialNativeQa.Run` (do **not** pass `-quit`; runner exits itself). It is `Assets/LocalBuild/Editor/SerialNativeQa.cs`, requires Application.isBatchMode, creates an empty scene, invokes production `LocalLlm.StartEditorQa`, performs real short chats, checks nonempty native callbacks and serial scheduling/cancellation, then stops its owned service. Run it serially with any other model/Unity process. Output: `LocalVerification/SerialNative/<UTC stamp>/result.json`, `passed.txt`, `owned-native` logs. Verify report.model equals the new exact name and bind report to deployed GGUF hash. This is the minimum existing test that proves a real reply with the actual runtime integration, beyond a metadata/load check.

Historical `LocalVerification/SerialNative/20261018-075016-103/result.json` is success for **Qwen3-4B**, CPU avx2; do not relabel it. `ChapterOneNativeChatQa` tests native UI components/scripted contacts and is not LLM-origin proof. `Tools/LlamaSharpProbe/Program.cs` is an old macOS LLamaSharp probe with hardcoded paths and a different native library, so it does not validate Windows LlamaLib 2.0.5. No existing standalone Windows non-Unity inference runner was found in Tools or coordination.

A final Player claim separately requires the current source build plus isolated `--verification-save-dir` run and matching ReleaseVerificationTrace native lifecycle/chat callback/bubble provenance. SerialNativeQa proves Editor integration only.
