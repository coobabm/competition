# Native runtime license provenance / 原生运行库许可来源

Retrieved 2026-10-18 from the upstream repositories. License texts are unmodified downloads.

Actual bundled native runtime: LlamaLib-v2.0.5, Windows CPU variants (avx, avx2, avx512, noavx, runtime).
Version evidence: LLMUnity v3.0.3 Runtime/LLMUnitySetup.cs LlamaLibVersion="v2.0.5".

- LlamaLib: Apache-2.0. Tag v2.0.5 resolves to commit 21d3b9dcd1c05ad13f94f198e59462960019a057.
  https://github.com/undreamai/LlamaLib/tree/21d3b9dcd1c05ad13f94f198e59462960019a057
  License: https://raw.githubusercontent.com/undreamai/LlamaLib/21d3b9dcd1c05ad13f94f198e59462960019a057/LICENSE
- llama.cpp: MIT. LlamaLib's actual gitlink pins 872646b30c342c5fa98efec002f94fdb4f1f1ea5.
  https://raw.githubusercontent.com/ggml-org/llama.cpp/872646b30c342c5fa98efec002f94fdb4f1f1ea5/LICENSE
- FeatureDetector: CC0-1.0. Actual gitlink pins 077393260d5e0e19938582a33b9dd0e74baa8ff2.
  https://raw.githubusercontent.com/Mysticial/FeatureDetector/077393260d5e0e19938582a33b9dd0e74baa8ff2/LICENSE
- JSON for Modern C++ (jsonhpp): MIT, license copied from that exact llama.cpp revision.
  https://raw.githubusercontent.com/ggml-org/llama.cpp/872646b30c342c5fa98efec002f94fdb4f1f1ea5/licenses/LICENSE-jsonhpp
- cpp-httplib: MIT, license copied from that exact llama.cpp revision.
  https://raw.githubusercontent.com/ggml-org/llama.cpp/872646b30c342c5fa98efec002f94fdb4f1f1ea5/vendor/cpp-httplib/LICENSE

Included API evidence: tag, release metadata, recursively listed source trees, and .gitmodules. There was no standalone NOTICE/THIRD_PARTY file at the LlamaLib tag; this README is a provenance index, not a claim that upstream authored it. LLMUnity's separate LICENSE.md and Third Party Notices.md are copied by Package-VerifiedRelease.ps1, including its own USearch license. Qwen3-1.7B model Apache license/model card remain beside the model.

Release metadata records official LlamaLib-v2.0.5.zip SHA256 f3d85b2d6fbaa12839173f4438a7a2c7124bf6b25708d8be23348de2dec1156b. The 1.8 GB runtime archive was NOT downloaded again for this license task; this metadata does not claim a new byte comparison of the installed DLLs against the archive.

Assets/StreamingAssets/LingGuang/llama/THIRD_PARTY.txt now identifies the current LlamaLib CPU runtime and Qwen3-1.7B model; its separately marked legacy entries describe retired payloads. That index does not substitute the authentic native license texts collected here.

Local release preparation, 2026-10-20: copied from C:\Users\xomem\Documents\ChatGPT\games\发布材料\Claude第二轮协作_20261019_183735\evidence\native-licenses . Only this provenance explanation was updated for the Qwen3-1.7B release; upstream license texts and collected evidence files are byte-identical to the source delivery. SHA256SUMS.txt was regenerated for this independent staging directory. No new upstream downloads or installed-DLL byte comparison were performed.
