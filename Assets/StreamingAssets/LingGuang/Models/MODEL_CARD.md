# 《我的电脑活了》本地模型

游戏离线运行两个模型，均由 LLM for Unity（LlamaLib v2.0.5，llama.cpp）在本机推理，不联网。

| 用途 | 文件 | 来源 | 许可 |
|---|---|---|---|
| 对话（晴雯、老周、灵光等） | Qwen3.5-2B-Q5_K_S-emb5.gguf，1.31 GB | Qwen/Qwen3.5-2B，本团队量化 | Apache-2.0（本目录 LICENSE） |
| 检索向量（RAG） | ../Embedding/bge-small-zh-v1.5-q8_0.gguf，26 MB | BAAI/bge-small-zh-v1.5，CompendiumLabs 转 GGUF | MIT（../Embedding/LICENSE-bge-small-zh-v1.5.txt） |

## 量化方式

为让整包小于 2 GB，从 bartowski/Qwen_Qwen3.5-2B-GGUF 的 bf16 出发：

1. 去掉 MTP（nextn）层（Tools/LocalLLM/strip_qwen_mtp.py；LlamaLib 的 llama.cpp 不认这一层，它只用于投机解码）。
2. 用 bartowski 的 imatrix 量化为 Q5_K_S，词表嵌入层（与输出层共用）用 Q5_K：
   `llama-quantize --imatrix imatrix.gguf --token-embedding-type q5_K <bf16-nomtp.gguf> Qwen3.5-2B-Q5_K_S-emb5.gguf Q5_K_S`

在游戏自己的中文文本（剧情、知识库，约 1.2 万 token）上与 bf16 比较的 KL 散度：

| 量化 | 大小 | KLD | 首选词一致 |
|---|---|---|---|
| Q4_K_M（常用） | 1.27 GB | 0.027 | 90.2% |
| **Q5_K_S + Q5_K 嵌入（采用）** | 1.31 GB | 0.012 | 92.9% |
| Q5_K_S | 1.37 GB | 0.009 | 94.2% |
| Q6_K | 1.56 GB | 0.0035 | 96.2% |

Qwen3.5-4B 最小的 IQ2_M 也有 1.95 GB，放不进 2 GB；同体积下 2B 的 5 bit 比 4B 的 2 bit 稳定。
完整步骤见 Tools/LocalLLM/quantize_qwen35_2b.sh。
