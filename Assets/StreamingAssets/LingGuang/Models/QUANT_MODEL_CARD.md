# Qwen3.5-2B-Q5_K_S-emb5.gguf — 量化与微调说明

- 基座：Qwen/Qwen3.5-2B（Apache-2.0），bf16 GGUF 与 imatrix 来自 bartowski/Qwen_Qwen3.5-2B-GGUF。
- 微调：本团队用游戏自己的中文对话（晴雯、老周、灵光和其他联系人，约 3,400 条）训练 LoRA（rank 16，全部 24 层），并合并进权重。
  流程与数据在 Exports/model-work/lora，留出提示上的对照见该目录的 report.md。
- 去掉 MTP（nextn）层：Tools/LocalLLM/strip_qwen_mtp.py。
- 量化：`llama-quantize --imatrix imatrix.gguf --token-embedding-type q5_K <bf16-nomtp.gguf> Qwen3.5-2B-Q5_K_S-emb5.gguf Q5_K_S`
  （Tools/LocalLLM/quantize_qwen35_2b.sh）。
- 文件：1,306,533,984 字节，SHA256 4b154ece943d21ebdf8179935a335cd35897babd212cb144a86a8605bf1782b1。
