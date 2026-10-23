# Qwen3-4B 一致性检查（claude-batch-02 / qwen3-consistency）

日期：2026-10-19　基线：`competition-latest-20261016`（只读，未改动）
发行模型（用户决定）：`Qwen3-4B-Q4_K_M.gguf`，2,497,280,256 字节，SHA256 `7485fe6f11af29433bc51cab58009521f205840f5b4ae3a32fa7f92e8534fdf5`，Apache-2.0，来源 https://huggingface.co/Qwen/Qwen3-4B 与 https://huggingface.co/Qwen/Qwen3-4B-GGUF 。`IntegratedReleaseBuild.cs` 已写死该文件名，并在构建后断言输出里**只有这一个** GGUF。

## 交付物

| 路径 | 内容 |
|---|---|
| `candidate/Assets/LingGuangV05/Core/Chat/LlmScheduling.cs` | 完整文件。仅改注释；`ServiceArgs()` 输出逐字节不变，现有测试无需改 |
| `candidate/Assets/LingGuangV05/Core/Chat/LlmPromptLayout.cs` | 完整文件。仅改类注释 |
| `candidate/Assets/StreamingAssets/LingGuang/llama/THIRD_PARTY.txt` | 完整文件。许可文本改为 Qwen3-4B |
| `qwen3-consistency.patch` | 手写 diff。Part A = 上面三份（已应用到 candidate）；Part B = 行为提案（**未应用**）；Part C = README 措辞建议（**未应用**，非本路所有） |

注：任务说明写的是 `LocalLLM\LlmScheduling.cs` / `LlmPromptLayout.cs`，生产中实际位置是 `Assets/LingGuangV05/Core/Chat/`，candidate 按实际路径镜像。

## 命中表（grep：Qwen3.5 / Qwen3_5 / qwen35 / 3.5-4B / Qwen_Qwen / bartowski / hybrid recurrent）

| # | 文件:行 | 分类 | 处理 |
|---|---|---|---|
| 1 | `Assets/LingGuangV05/Core/Chat/LlmScheduling.cs:29` | 注释（描述了一项行为假设：混合架构 checkpoint） | 已改注释；行为分析见 B1 |
| 2 | `Assets/LingGuangV05/Core/Chat/LlmPromptLayout.cs:8` | 注释（同上） | 已改注释；布局对 Qwen3-4B 仍正确 |
| 3 | `Assets/StreamingAssets/LingGuang/llama/THIRD_PARTY.txt:36` | 许可文本 | 改为“本版无旧模型，唯一模型为上方 Qwen3-4B”；顶部补 base model 链接、字节数、“不带向量模型” |
| 4 | `Assets/LingGuangV05/LocalLLM/LocalLlm.cs:31` | 注释 | **不改**（另一路负责） |
| 5 | `Assets/LingGuangV05/LocalLLM/LocalLlm.cs:291` | 行为相关日志（仅 `LogWarning`，不影响流程） | **不改**（另一路）。建议：Qwen3-4B 不含 MTP 层，出现 `missing tensor 'blk.` 应提示“模型文件损坏或与运行库不匹配”，而不是指向 strip 脚本 |
| 6 | `Main-source/Assets/LingGuangV05/LocalLLM/LocalLlm.cs:21` | 注释（旧副本目录） | 不改；提醒协调者此副本是否仍参与构建 |
| 7 | `Assets/LingGuangV05/README.md:87,90` | 文档 | 不在本路所有权；Part C 给出建议 |
| 8 | `Assets/LingGuangV05/XingGuang/README.md:139,141,144` | 文档（整节还描述已废弃的 llama-server:18088） | 同上 |
| 9 | `Tools/LocalLLM/strip_qwen_mtp.py:1,9` | Qwen3.5 专用工具 | 不改；对 Qwen3-4B 无用，建议标注 legacy |
| 10 | `Tools/LlamaSharpProbe/Program.cs:10,13` | 一次性探针（Mac 绝对路径） | 不改，历史工具 |
| 11 | `Documentation/LingGuang-Incremental/*.md`（v1.1落地计划 961/1035/1072/1090；v0.2对照 30；女友系统 358；人机协作标注 7/11） | 历史实测记录 | 不改：记录的是当时在 Qwen3.5 上的实测，改名会失真。v0.2对照:30“以实际文件 Qwen3.5-4B 为准”已与现决策相反，建议协调者在该文加一行勘误 |
| 12 | `Design/*.md`（3 处） | 生产 Design | 按要求不改；内容已正确说明“当前用 Qwen3-4B” |

已核对无问题：`Models/SHA256SUMS.txt`、`Models/MODEL_CARD.md`（Qwen3-4B-GGUF 原卡）、`Models/LICENSE`（Apache-2.0）、`IntegratedReleaseBuild.cs`、`LlmPersonas.cs:208`（只是让 NPC 不认识 “Qwen” 一词）。

## 行为分析

### B1. KV / 槽位内存（Qwen3-4B：36 层，GQA 8 KV 头，head_dim 128）

每 token K+V 元素数 = 2×36×8×128 = 73,728。

| 缓存类型 | 每 token | 每槽 4096 | 3 槽（-c 12288） | 4 槽（-c 16384，并行判题） |
|---|---|---|---|---|
| f16（仅 GpuPlainCache） | 144 KiB | 576 MiB | 1728 MiB | 2304 MiB |
| q8_0（Gpu、**Cpu** 默认） | 76.5 KiB | 306 MiB | 918 MiB | 1224 MiB |

代码：`-c = 4096 × slots`，`-np slots`，Cpu 与 Gpu 模式都用 q8_0 KV。代码里**没有**显式的内存预算公式，所以没有“算错”的地方；但原调参针对 Qwen3.5 混合架构（只有少数层是全注意力，KV 约为同规模纯注意力模型的 1/4 量级——具体层配置未核实），换成 Qwen3-4B 后同样的 `-c` 实际 KV 约大 4 倍。

Windows 发行版走 CPU：权重 2.33 GiB + KV 0.90 GiB（3 槽）+ `--cache-ram` 上限 2 GiB ≈ **5.2 GiB 最坏值**（未计计算缓冲与 Unity 本体）。8 GB 内存机器偏紧。
- `SlotContext=4096` 合理（最大请求约 1.1k token，另需给第 6 阶段思考输出留量），**不建议改**。
- `--fit-ctx 4096`：因 `-c` 已显式给出，fit 不会动 ctx，只调层数——对 Qwen3-4B 行为正确。
- **提案 Part B（未应用）**：`CacheRamMiB 2048 → 1024`（同步改 `LlmSchedulingTests` 一处断言）。它是上限而非预分配；Qwen3-4B 一份 1.1k token 提示的 q8_0 状态约 84 MiB，1 GiB 仍可存约 12 份，覆盖 3–4 个座位绰绰有余。合并前需实机测峰值内存。

### B2. checkpoint / cache-reuse
- `--ctx-checkpoints 6`：llama.cpp 只为混合/递归/SWA 模型做 context checkpoint；对 Qwen3-4B 应为无效果（无害，测试仍断言该参数，保留）。建议实机看一次日志确认无 checkpoint 行。
- `--cache-reuse 256`：原注释说对混合模型被忽略；对 Qwen3-4B **会生效**（KV 平移复用）。属于性能改善，未见正确性风险。
- `LlmPromptLayout`（系统消息不变、历史只增长、可变状态放最后一条 user、窗口按半步跳）对纯注意力模型同样是最优前缀复用布局，**无需改代码**。

### B3. 思考模式
- `--jinja --reasoning-budget 0` + `service.EnableReasoning(false)` → 模板以 `enable_thinking=false` 渲染；Qwen3-4B 官方模板此时追加 `<think>\n\n</think>\n\n`，与 `ClosedThinking` 完全一致，`WithThinking` 正则匹配后替换成自身——**正确**。
- `thinking=true` 时改为 `<think>\n`，Qwen3 支持该写法；`AfterThinking`/`Clean` 剥离 `<think>` 块也适配 Qwen3 输出。
- 未使用 `/think`、`/no_think` 软开关，不需要；玩家输入中出现 `/think` 字样理论上可能触发模型思考（低风险，见问题 3）。

### B4. 向量模型（Qwen3-Embedding-0.6B）与 BM25 回退
- 生产 `StreamingAssets/LingGuang/Embedding/` 为空（只有 .meta）；`LocalEmbedder.FindModel()` 返回 null → 记日志、`RagVectors.Source` 保持 null → `HybridScorer` 退化为**纯 BM25**（Rag.cs 注释与 RagTests 已覆盖）。一致。
- `IntegratedReleaseBuild` 断言输出里只有一个 GGUF：若有人把向量 GGUF 放进 StreamingAssets，**发行构建会直接失败**。因此“正式包恒为 BM25”由构建强制保证，与交接文档一致。
- 文档/注释（`LingGuangV05/README.md:98`、`Rag.cs:104`、`MemoryRetrieval.cs:31`、`LocalEmbedder.cs:22`）把 Qwen3-Embedding 写得像已随游戏提供；建议注明“可选，发行包不带”（Part C 只给了 README 的建议，.cs 不在本路所有权）。
- 若将来要带向量模型：额外约 0.6 GB 权重 + 2048 ctx 的 KV（28 层×8 头×128，f16 约 224 MiB），且需同时放宽构建断言、更新 THIRD_PARTY 与 SHA256SUMS。

## 待确认问题
1. Part B（cache-ram 减半）是否采纳？需要谁做一次 Windows CPU 包的峰值内存实测（3 槽 / 4 槽并行判题各一次）？
2. `Main-source/` 目录下的旧 `LocalLlm.cs` 是否仍参与任何构建？若否，建议整体标为归档。
3. 是否需要在玩家输入进入 prompt 前转义 `/think`、`/no_think`？（Qwen3 会响应用户消息中的软开关）
4. 采样参数：Qwen3 官方建议非思考模式 temp 0.7 / top_p 0.8 / top_k 20 / presence_penalty 1.5（量化模型），思考模式 temp 0.6 且禁止贪心解码。本次未核对 `ChatParameters`/`XgSampling` 的取值，是否另开一路检查（尤其第 6 阶段 thinking=true 时若为低温/贪心，可能出现重复）？
5. `Documentation/…/策划案v0.2落地对照.md:30` 写“以实际文件 Qwen3.5-4B 为准”，与现决策相反，是否由协调者加勘误？
