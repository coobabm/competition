# tech-049：subword（子词切分）核对结果

结论：需要小范围补全文案；不改模拟数值。当前 XgTechTreeCopy 的通用训练处理量 ×1.05 是正确的，但未交代购买即持有、无需装备、两条训练线、结算时读取和乘法叠加。通俗解释增加“原理是”，避免把真实 AI 技术解释当作游戏已经运行的分词功能。名称“子词切分 / Subword units”可保留，不为改名制造补丁。

范围仅为 subword。没有读取新的策划正文来推导需求；一次关键词搜索意外返回清单中的单行命中，未继续展开或作为结论依据。已检索 Parallel100 现有记录，未发现 subword/tech-049 已完成产物。本目录此前不存在。

## 可直接采用的中英文文案

中文正式名称：子词切分

English name: Subword units

中文解释：子词切分的原理是：把生僻词拆成常见的小片段，像用熟悉的零件拼出新词。

English explanation: Subword tokenization means splitting rare words into familiar pieces, like building new words from known parts.

中文效果：购买后自动生效，无需装备；视觉和序列训练每轮结算时，取整前卡数 ×1.05（+5%），与其他训练倍率相乘，每项只计一次。已开始但未结算的轮次也适用；每轮仍受算力、取整和 4–320 张限制，轮次用时不变。成绩需训练后评估，不直接增加 5 个百分点。

English effect: Active on purchase; no equip step. At each vision or sequence epoch settlement, pre-rounding card count is multiplied by 1.05 (+5%), stacking multiplicatively with other training factors, once per item. This includes epochs already in progress. Compute, rounding and the 4–320 cards/epoch limits still apply; epoch duration is unchanged. Train and evaluate to see score changes; accuracy does not gain 5 percentage points.

## 真实代码依据

以下路径均相对工程根目录；对应完整 SHA256 见 source-hashes.json。

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:450,610`：subword 是 Research 节点，研究类别 Feel，前置 attention，阶段 5；不是可装备的模型架构。目录标价 9000，实际价格经 NodeCost 计算，文案补丁不硬编码价格。
- `Core/XgSim.Progression.cs:140–149`（同一 XingGuang 目录）：未持有时仅 node.stage <= S.stage 可见；阶段到达只是可见，不等于得到效果。
- `Core/XgSim.cs:188,194–206,252–279`：Has 读取 unlocked；购买须满足可见、前置、资金等条件；成功购买写入 unlocked。已拥有节点不能再次购买。没有 subword 装备开关或等待训练完成才获得所有权的条件。
- `Core/XgSim.Collection.cs:70–82`：FeelSpeed 检查 Has("subword") 后乘 1.05，和其他已拥有 Feel 项依次相乘，没有文字/数据集/架构限定；同一项只有一次条件判断。
- `Core/XgSim.cs:347,554–557`：SpeedResearch 含 FeelSpeed，亦进入 StepsPerSecond 的通用公式；这不等于实际轮次计时缩短。
- `Core/XgSim.Board.cs:191–199`：正常 CardsPerEpoch 同时用于视觉和序列、手动和自动训练；先应用 SpeedResearch，之后 Math.Round，最终夹在 4–320 张。+5% 是无量纲乘数作用于取整前张数；不是准确率百分点，不保证可见卡数严格多 5%。
- `Core/XgSim.EpochAction.cs:9–25,28–48`：BeginEpoch 只固定轮次时长、不预先固定卡数；完成计时后调用 TrainEpoch。subword 不改变 DurationFor。
- `Core/XgSim.cs:638–669`：TrainEpoch 结算时计算 CardsPerEpoch，再训练并评估；因此结算前已购入即可纳入该轮倍率，进行中的轮次也可能适用。
- `Core/XgSim.Board.cs:193`：内部 OfflineSimulation 模式直接返回 12 张，不经过上述倍率。这是验证环境例外，不应把正常玩家文案承诺延伸到离线模拟；不是退出游戏后的挂机加成。
- `Core/XgSim.Finale.cs:75–85`：ScaleItems 无 subword，不增加预训练有效规模乘数；本节点没有额外能力阈值折扣的 subword 引用。源码全文 subword 命中只涉及目录、说明及 FeelSpeed，没有真实子词词表构建/分词训练入口。
- `Core/XgTechTreeCopy.cs:15,51` 保留 mystery/atlas 提前返回；`Core/XgTechTree.cs:127–130` 及 `Desktop/XgTechTreePage.cs:428` 保留阶段可见性；`Desktop/XgTechTreePage.cs:747–765` 保留未发现图鉴及 mystery 分支。补丁不改变这些规则，也不扩展未发现条目的内容。

## 补丁与整合

仅交付 `changes.patch`，未改生产文件。

目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`

生成时原文件 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`

补丁包含两个带上下文的片段：替换 subword 的解释；将 subword 从共享效果分支单列，sft/cot 的返回值完全不变。不改通用 Speed，不改目录旧文案，不改其他词条。

整合负责人采用前核对 SHA256/上下文；若其他窗口已编辑该文件，按 subword 的两个分支重定位，不能覆盖整个文件。目录旧 effect 仍写“文字训练 +5%”，此次授权仅修正科技树显示，XgTechTreeCopy 已覆盖该页；其他页面复用目录文案的清理由整合负责人另行判定，不在本任务中扩展。

## 验证状态和待验证边界

静态检查已完成：追踪购买→Has→FeelSpeed→SpeedResearch→CardsPerEpoch→TrainEpoch，确认两条训练线和结算时机；补丁 `git apply --check --ignore-space-change --ignore-whitespace` 成功，未执行 apply。未编译 C#，未运行 Unity/游戏，未作视觉验收，未读取玩家存档；不标运行 PASS。

下列是交给主聊天的实际验证项，尚未执行：

1. 阶段 4 未持有时不显示 subword；阶段 5 缺 attention 时不能购买；可见但未购时没有倍率。
2. 隔离夹具固定其他配置，分别比较视觉/序列和手动/自动的购前购后卡数，核对原始张数乘 1.05；不要允许自动模型变化污染对照。
3. 取整与上下限：原始 4→4.2 应仍为 4；10→10.5 按 Math.Round 偶数舍入仍为 10；100→105；320→336 应封顶 320。这些是公式推导的预期，未运行游戏验证。
4. 与另一个 1.05 因子组合时应为 1.1025，不是 1.10；重复购买不得重复扣费或再次叠加。
5. BeginEpoch 后、结算前购买，确认该轮结算读到新倍率，但已记录轮次时长未减；仅解锁可见不生效。
6. OfflineSimulation=true 应仍固定 12，防止用该模式误判加速失效；正常场景才验证玩家效果。
7. 中英文提示完整可读、长文不溢出；未发现节点保持原显示；购买不承诺立即提分。

最终依赖复核记录见下节。

## 结束前依赖复核

2026-10-13：重新计算 source-hashes.json 中全部 10 个依赖文件的 SHA256，与记录一致；本轮结论未因并行更新失效。
