# Windows 初版模型与许可只读交付审查

审查日期：2026-10-03。只读核对本地文件和现有构建代码；没有联网推断许可，没有触发 Unity 命令。下表仅建议复制，审查者没有改变 Assets、Packages 或最终包。

## 运行资源与路径

- `MainBuilder.BuildWindows` 明确传入唯一场景 `Assets/LingGuangV05/Scenes/Main.unity`，输出 `C:/Users/xomem/Documents/ChatGPT/games/Builds/LingGuangV06/LingGuang.exe`。旧 EditorBuildSettings 场景列表不会替换该显式参数。
- Main 场景的 `decisionModel` GUID 是 `236d3fedd348a0c47ab05dfce6a18be3`，与 `AI/Laya/laya_fp16.sentis.meta` 一致；模型大小 644,339,268 字节。`tokenizer` GUID 是 `6c8264318ddf4fd49a191f5eb0fb8f38`，与实际 tokenizer 匹配。这两项作为场景引用被 Unity 打入数据包，无须独立复制至 StreamingAssets。
- Qwen 文件 `Assets/StreamingAssets/Qwen3-4B-Q4_K_M.gguf` 为 2,497,280,256 字节，匹配 Talk 的完整性检查。运行路径是 `LingGuang_Data/StreamingAssets/Qwen3-4B-Q4_K_M.gguf`。`LLMManager.json` 当前 `downloadOnStart=false`，Qwen `includeInBuild=true`。
- LLMUnity 构建钩子会保留 `StreamingAssets/LlamaLib-v2.0.5/win-x64/native`，暂时移出其他平台，构建后复原。CPU noavx/avx/avx2/avx512 与 runtime DLL 本地均存在；Talk 设置 CPU。
- Unity 会包含所有 Resources 资源，不仅 Main 直接引用：仍有 Emergence 字体和 TMP 示例字体。没有发现 Resources 下额外的大模型。不要把“唯一 Main 场景”解释为“只打包 LingGuangV05 目录”。
- 以上静态检查没有发现导致模型路径失效的确定问题；最终生成文件与 Windows player 原生推理仍应以主会话实际构建/运行结果为准。

## 已有本地许可复制清单

建议目的地为 `C:/Users/xomem/Documents/ChatGPT/games/Builds/LingGuangV06/Licenses/`，保留各文件完整原文。目的文件名可如下区分来源。

| 本地原文准确路径 | 建议目的文件名 | 已核对内容与范围 |
| --- | --- | --- |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Assets/StreamingAssets/Qwen3-LICENSE.txt` | `Qwen3-LICENSE.txt` | Apache 2.0；已经在 StreamingAssets 随包，可额外集中展示。与 `C:/Users/xomem/Documents/ChatGPT/games/Models/Qwen3-4B/LICENSE` SHA256 完全相同：`5DE36594C10839788A8C589443A8EF9D8B8D17C65A1B5807206AE037FC36C6BD`。 |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Packages/ai.undream.llm/LICENSE.md` | `LLMUnity-LICENSE.md` | Apache 2.0，LLMUnity 包许可。Packages 文本不会自动成为用户可见许可文件。 |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Packages/ai.undream.llm/Third Party Notices.md` | `LLMUnity-Third-Party-Notices.md` | 本地上游声明含 llama.cpp MIT、llamafile Apache 2.0 等。其模型列表是插件可选下载目录，不能宣称列出的其他模型已随游戏分发。部分条目仅链接原许可，并非完整许可正文。 |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Packages/ai.undream.llm/Runtime/RAG/usearch/LICENSE` | `USearch-LICENSE.txt` | Apache 2.0；保留包内已有依赖许可。 |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Library/PackageCache/com.unity.ai.inference@9a123aee5df7/LICENSE.md` | `Unity-Inference-Engine-LICENSE.md` | Unity Terms of Service 引用；原文件注明 Sentis copyright 2025 Unity Technologies。 |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Library/PackageCache/com.unity.ai.inference@9a123aee5df7/Third Party Notices.md` | `Unity-Inference-Engine-Third-Party-Notices.md` | 含 Google.Protobuf、ONNX、MSAGL 等上游声明。 |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Assets/LingGuangV05/Art/Resources/LingGuangArt/Font/OFL.txt` | `NotoSansCJK-OFL.txt` | SIL Open Font License 1.1；字体构建器从 Emergence 的 NotoSansCJKsc-Regular.otf 与 OFL 复制产生当前字体。 |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` | `LiberationSans-OFL.txt` | TMP Resources 中存在 LiberationSans SDF，应保留现有对应 OFL。 |
| `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/Assets/TextMesh Pro/Examples & Extras/Fonts/Unity - OFL.txt` | `TMP-Unity-Font-OFL.txt` | TMP 示例 Resources 中存在 Unity SDF，应保留现有对应 OFL。 |

可另外保存以下导出工具的来源证据，必须使用准确标签，不将它当作模型权重许可证：

- `C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/改版方案/模型/laya_export/.venv/Lib/site-packages/laya-0.3.23.dist-info/licenses/LICENSE` → `Laya-Python-Package-LICENSE.txt`。原文 Apache 2.0；相邻 `METADATA` 明确写 Name=laya、Version=0.3.23、Author=Convai Innovations、License=Apache-2.0。该 Python 包用于导出，不是独立证明权重许可的文件。

## 尚未有本地原文证据的部分

1. `convaiinnovations/laya-multilingual` 权重许可未在本地找到。导出源码明确指定此模型；本地缓存 snapshot 为 `e4e9ddf21a7b1903b7acffd8814ad4307bf63a67`。
2. 精确缓存目录：`C:/Users/xomem/Documents/ChatGPT/games/RipplePrototype/改版方案/模型/laya_export/.venv/cache/huggingface/hub/models--convaiinnovations--laya-multilingual/snapshots/e4e9ddf21a7b1903b7acffd8814ad4307bf63a67/`。它只含 model.safetensors、rl_agent_config.json、encoder/config.json、tokenizer/tokenizer_config.json、tokenizer/tokenizer.json，没有 LICENSE、NOTICE 或 README 模型卡。
3. `rl_agent_config.json` 指定基础 encoder `jhu-clsp/mmBERT-base`；在本次检查的本地模型缓存/导出目录未找到其独立许可原文。不能从 Laya Python 包的 Apache 2.0 推断两组权重的许可。
4. LLMUnity 的 Third Party Notices 对 llama.cpp 提供 MIT 名称与链接；已检查的插件包与本地工具副本没有发现其完整 MIT 原文。可以保留现有上游 notices，但不能记录为已经补齐全部上游许可正文。

这些是交付来源记录的待补项，不是本地原型运行失败的证据。记录已有证据即可，不要为了标记完成而将工具许可误标为模型权重许可。
