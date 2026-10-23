# Qwen3.5-4B records audit — 2026-10-19

Read-only inventory; no production files, historical packages, archives, model weights, or licenses were edited. No Unity, model download, commit, or large-model hash was run. All paths below are relative to `C:/Users/xomem/Documents/ChatGPT/games/` unless prefixed with `SOURCE/`; `SOURCE/` means `competition-latest-20261016/`.

## Observed identity versus requested target

- Requested target: Qwen3.5-4B. Final GGUF filename, upstream repository/revision, original versus MTP-stripped status, final byte length and SHA256 remain to be established by the model owner before production record changes.
- Observed active source model: `SOURCE/Assets/StreamingAssets/LingGuang/Models/Qwen3-4B-Q4_K_M.gguf`, 2,497,280,256 bytes. Existing recorded SHA256: `7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5`. This audit read the recorded checksum and file length, not the complete model bytes.
- Source build currently uses version `2026.10.19.3`; the .3 packager pins that Qwen3 path and checksum. Historical .3 artifacts must retain their actual Qwen3 identity. Prefer a new release/package version and fresh package-tool directory for the new target.
- No AGENTS.md was found by `rg --files -g AGENTS.md` from workspace root.

## Active executable/configuration inputs requiring synchronized changes

| Exact path | Finding / required treatment |
|---|---|
| `SOURCE/Assets/LocalBuild/Editor/IntegratedReleaseBuild.cs` | Line 24 version `.3`; line 35 hardcodes `Qwen3-4B-Q4_K_M.gguf`; lines 41-44 normalize only entries with that exact basename. Change to verified target and new version. Explicitly account for stale Qwen3 entries in shared editor preferences without persisting unrelated user changes; preserve finally restoration and one-GGUF output check. |
| `SOURCE/Assets/StreamingAssets/LLMManager.json` | Lines 6-7 hardcode Qwen3 path; embeddingLength=2560 and contextLength=40960 belong to current entry. Update model identity and metadata from actual target model/config, not guessed Qwen3.5 values. |
| `SOURCE/Assets/LingGuangV05/LocalLLM/LocalLlm.cs` | Line 31 default-model comment says Qwen3-4B. `FindModel()` at 448-458 picks the first non-mmproj GGUF >100 MB in the Models directory; merely adding Qwen3.5 beside Qwen3 can continue loading Qwen3. Canonical selection or exclusion of old weights from active StreamingAssets must be explicit. Preserve original weights outside active model scan/build tree. Line 291 is a concrete Qwen3.5 missing-tensor diagnostic, not a stale label to remove blindly. |
| `SOURCE/Assets/StreamingAssets/LingGuang/Models/SHA256SUMS.txt` | Replace with final target basename and independently established final-file checksum. If transformed, record both upstream and final hashes in provenance. |
| `SOURCE/Assets/StreamingAssets/LingGuang/Models/MODEL_CARD.md` | Entire card is Qwen/Qwen3-4B-GGUF. Replace with authentic Qwen3.5 target card plus separately labeled conversion provenance; do not globally rename upstream card text. |
| `SOURCE/Assets/StreamingAssets/LingGuang/Models/LICENSE` | Existing upstream license includes model-specific Alibaba appendix. Fetch/copy actual target upstream license unchanged; verify license rather than synthesizing one. Existing Unity .meta can remain for retained text asset paths. |
| `SOURCE/Assets/StreamingAssets/LingGuang/llama/THIRD_PARTY.txt` | Lines 5-9 current model/source/hash must match target; line 30 current Qwen3 distinction needs rewording. Lines 29-36 legacy bundle facts remain historical and conditional. Identify actual quantizer and transformation, rather than assuming historical bartowski payload matches new file. |
| `.coordination/resume-20261019/claude-takeover/package-2026.10.19.3/Package-VerifiedRelease.ps1` | Lines 45,53 path/hash validation; line 209 generated Chinese README says Qwen3. Line 59 warning concerns retired runtime notices and should refer to retired server bundle rather than implying every Qwen3.5 notice is stale. Clone to next version, update verified identity, and retain one-GGUF guard. Pin MODEL_CARD/SHA256SUMS/provenance correspondence as well as LICENSE where practical. |
| `.coordination/resume-20261019/claude-takeover/package-2026.10.19.3/Package-2026.10.19.3.ps1` | No model literal; hardcoded .3 accepted version/output path must change in next-version launcher. |
| `.coordination/resume-20261019/claude-takeover/package-2026.10.19.3/README.md` | Line 21 records old source/hash/length and line 43 describes canonical check. Preserve historical .3 README; describe new verified target in new-tool README. |
| `.coordination/resume-20261019/claude-takeover/run-build-3.ps1` | No model literal. New build wrapper should use fresh log/snapshot names and correct release identity. Preserve old build evidence. |

The new model's actual GGUF and `.meta` are new payload inputs, not text substitutions. `.gitignore` already excludes model GGUF and its `.meta`; retain this exclusion.

## Active documentation requiring current-version synchronization

| Exact path | Required treatment |
|---|---|
| `SOURCE/Design/发布材料/发行包_README_候选.txt` | Lines 13,101,115 name Qwen3, with ~2.5 GB at 13. Use final Qwen3.5 filename/size, accurate runtime behavior, and correct new candidate version/verification limits. |
| `SOURCE/Design/发布材料/GitHub更新说明_候选.md` | Lines 13,96 model and license identity; update to verified target and current version without upgrading unverified performance claims. |
| `SOURCE/Design/发布材料/许可映射表.md` | Lines 18,23,75 model/path/hash and “本版不含…Qwen3.5”; synchronize target and distinguish old server runtime from the new model. |
| `SOURCE/Assets/LingGuangV05/README.md` | Already claims Qwen3.5 at 87 but old `Qwen_Qwen3.5-4B-Q4_K_M.gguf` path/MTP statement at 90. Clarify actual selected target and whether stripping was necessary. Line 98 claims embedding model present: current Windows package has none, so label embedding optional and BM25 current if still true. |
| `SOURCE/Assets/LingGuangV05/XingGuang/README.md` | Lines 139-144 already Qwen3.5 but old prefixed path/~3.0 GB and older runtime description. Reconcile exact file, actual size and LLMUnity runtime with primary README. |
| `发布材料/TapTap素材说明填表稿_我的电脑活了_20261019.md` | Line 56 current attribution says Qwen3-4B-Q4_K_M. Update to final target while retaining third-party attribution obligation. |
| `发布材料/开发日志NO5_全局修补优化与DEMO展示_续写稿.md` | Lines 73-74 explicitly describe previously published Windows release. Preserve those dated observations and add current target/new package status; do not rewrite old release as Qwen3.5. |

`发布材料/TapTap上传逐步填写稿_我的电脑活了_20261019.md` has no Qwen-specific literal. Other source `Design/发布材料/` drafts contain no Qwen3 literal in scoped scan; review new version/link text when publishing, not global replacement.

## Handoffs / integration history: add supersession notice, retain observations

Suggested notice, filled only with facts available at time of writing:

> 模型配置更新（2026-10-19）：用户现要求后续发行包使用 Qwen3.5-4B。本文下方的 Qwen3-4B 路径、哈希、包体和性能结果是当时记录，保留原样，不作为新包配置。当前目标文件、来源、校验值及实测状态见 [当前模型迁移记录]。新包完成前不得将此前 Qwen3 验证结果表述为 Qwen3.5 已验证。

Exact source handoffs carrying old active instructions:

- `SOURCE/Design/Claude环境限制与整合纠偏记录_20261019.md` — line 22 explicitly says not to upgrade Qwen3.5; supersede this now-obsolete instruction prominently.
- `SOURCE/Design/Claude协作交接_20261019_暂停后最新版.md` — line 101 canonical old filename/size/hash.
- `SOURCE/Design/Claude交接_20261019/交接总表与提示词.md` — lines 36,88 canonical old package/model rules.
- `SOURCE/Design/Claude主导协作交接_20261019.md` — line 186 canonical old filename/size/hash.
- `SOURCE/Design/Claude协作分工_剩余任务各半_20261019.md` — line 61 old release-description requirement.
- `SOURCE/Design/本轮交付剩余清单_20261019.md` — line 12 old version, model and verification task instructions.
- `SOURCE/Design/并行恢复整合记录_20261019.md` — line 27 factual old comment correction; retain and append migration entry.
- `SOURCE/Design/整合记录.md` — line 124 actual Qwen3 CPU pass; line 710 actual old notice update. Retain all historical records and append new migration/result entry.
- `SOURCE/Design/主标题点缀与2026.10.19.2包体说明.md` — line 9 names .2 Qwen3 package. Retain .2 fact; add newer package pointer if this remains a current handoff.
- `SOURCE/Design/老周知识问答与2016科技资讯_跨会话策划交接_2026-10-19.md` — lines 523/531 prior published-release observation and warning. Retain facts and add new target/current validation note.
- `发布材料/老周知识问答与2016科技资讯_跨会话策划交接_2026-10-19.md` — matching copy; same additive notice.
- `SOURCE/Design/老周知识策划_更新差异_20261019.md` — line 23 old scope decision; retain historic decision, append new scope pointer if used as latest handoff.
- `.coordination/resume-20261019/codex-resume-review/release.md` — line 29 historic read-only model audit, do not overwrite evidence; add supersession pointer if active.

Preferred new current source record: `SOURCE/Design/Qwen3.5-4B模型迁移与发行记录_20261019.md` with target filename, upstream repo + revision + original file/hash, optional transformation tool/command + final hash/size, license/card sources, runtime version, updated build/package version, validation actually completed, old .3 package location and truthful supersession status. Keep root publication copy synchronized if created.

## Tests / existing Qwen3.5 implementation references

- `SOURCE/Assets/LingGuangV05/Tests/Editor/LlmSchedulingTests.cs` uses generic `/m/q.gguf`, no old model identity; do not rename unrelated tests.
- No Qwen3-specific literal found elsewhere in `Assets/LocalBuild` tests/checks besides `IntegratedReleaseBuild.cs`.
- `SOURCE/Assets/LingGuangV05/Core/Chat/LlmScheduling.cs:29` and `LlmPromptLayout.cs:8` already document Qwen3.5 hybrid architecture; keep if runtime target supports those assumptions.
- `SOURCE/Tools/LocalLLM/strip_qwen_mtp.py` is a real Qwen3.5 conversion tool. Its sample old prefixed basename may be updated to selected filename if used; do not claim conversion happened without output provenance.
- `SOURCE/Tools/LlamaSharpProbe/Program.cs:10,13` is a Mac feasibility probe with old absolute Qwen3.5 path. It is not active packaging. Optionally make a portable model argument, but no release requirement justifies unrelated probe changes.
- `LocalEmbedder.cs`, `Core/Chat/Rag.cs`, `MemoryRetrieval.cs` refer to Qwen3-Embedding-0.6B, a different model family/task. Do not replace these with Qwen3.5-4B.
- Model-switch verification should establish actual selected filename plus native architecture at startup and one real nonfallback reply, rather than only testing strings. Source text changes alone do not demonstrate runtime compatibility.

## Explicit historical exclusions

Do not edit archives or old packaged contents, including root `MyComputerAlive-2026.10.19.3-package`, any existing `Build/` output, root `发布材料/Claude*` handoff snapshots/zips, `.coordination` candidate snapshots, `Main-source/`, `Artifacts/`, `LocalVerification/`, and historical releases `SOURCE/Documentation/Releases/2026.10.11.2/{README.md,package.json}` and `SOURCE/Documentation/Releases/2026.10.12.2/README.md`. `SOURCE/Documentation/Handoff/2026-10-17/Windows-crash-fix.md` and older design/devlog observations must retain the actual model tested then. Existing 7z checks, old source-provenance and model checksum records describe immutable previous outputs.

Current source comments/design references mentioning Qwen3/Qwen3.5 generically or Qwen3 embeddings are not incorrect current-package identity claims. A complete synchronization means clear current authority plus truthful historical supersession, not zero search hits for Qwen3.
