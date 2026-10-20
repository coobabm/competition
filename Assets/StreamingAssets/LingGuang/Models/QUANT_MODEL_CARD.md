# Qwen3.5-2B-Q5_K_S-emb5.gguf — 量化与微调说明

- 基座：Qwen/Qwen3.5-2B（Apache-2.0），bf16 GGUF 与 imatrix 来自 bartowski/Qwen_Qwen3.5-2B-GGUF。
- 微调：本团队用游戏自己的中文对话（晴雯、老周、灵光和其他联系人，约 3,400 条）训练 LoRA（rank 16，全部 24 层），并合并进权重。
  流程与数据在 Exports/model-work/lora，留出提示上的对照见该目录的 report.md。
- 去掉 MTP（nextn）层：Tools/LocalLLM/strip_qwen_mtp.py。
- 量化：`llama-quantize --imatrix imatrix.gguf --token-embedding-type q5_K <bf16-nomtp.gguf> Qwen3.5-2B-Q5_K_S-emb5.gguf Q5_K_S`
  （Tools/LocalLLM/quantize_qwen35_2b.sh）。
- 文件：1,306,533,984 字节，SHA256 4b154ece943d21ebdf8179935a335cd35897babd212cb144a86a8605bf1782b1。

## 可复现性与发行文件

当前发行文件是上游 Git LFS 中已合并 LoRA r2 的成品，以本目录 SHA256SUMS.txt 为准。当前仓库不含 Exports/model-work/lora 中提到的 adapter、训练数据版本、训练配置、合并步骤及 report.md，也未记录基座、imatrix 和量化工具的精确 revision，因此不能仅用公开基础模型复现该 SHA256。

Tools/LocalLLM/quantize_qwen35_2b.sh 仅用于在另行取得原始 LoRA 合并后 bf16、imatrix 和匹配工具链时生成候选量化文件；它不会下载基础模型代替 LoRA，也不会覆盖 Assets 中的发行模型。候选文件仍须通过发行 SHA256 检查。BGE 成品同样由 Git LFS 提供并在 SHA256SUMS.txt 中校验。
