# GRU 可直接采用的文案

## 中文

正式名称：门控循环单元（GRU）

通俗解释：边读边筛选记忆：学到有用的线索就多留一会儿，其余线索更快淡去。

效果：购买后加入序列模型候选，无需手动装备；系统按数据和显存自动选用，正在训练时等本轮结束再切换。选用后，每轮训练卡数的结构系数为 ×1.05（LSTM 为 ×0.8），与其他处理量倍率相乘，不与其他结构叠加；实际卡数仍受取整和上限影响。同层数、同宽度时，参数量为 LSTM 的 75%。门控记忆与 LSTM 相同，仍会随距离衰减。换结构可能重置学习进度，需继续训练并评估，不保证立即提分。

## English

Formal name: Gated recurrent unit (GRU)

Plain explanation: Filter memory while reading: keep clues the model has learned are useful for longer, and let other clues fade faster.

Effect: Buying adds GRU to the sequence-model candidates; no manual equip is needed. The system selects a model for the data and VRAM, waiting until the current epoch ends before switching. When selected, its architecture factor for training cards per epoch is ×1.05 (LSTM: ×0.8). This multiplies other throughput factors, but does not stack with other architectures; rounding and caps still apply. At the same depth and width, it has 75% of LSTM's parameters. It shares LSTM's gated-memory rule and still fades with distance. Switching architecture may reset learning progress; continue training and evaluate, with no guaranteed immediate score gain.
