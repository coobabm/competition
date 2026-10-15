# tech-002：LSTM 词条核对

日期：2026-10-13（Asia/Singapore）。结论：**需要两处仅文案修正，待整合**。没有修改生产源码、模拟数值或真实存档，没有启动 Unity/游戏。只交付本目录文件。

## 结论与已有工作

已核对 `Documentation/DesignPass/Agents/skilltree-task-001.md`，已有统一文案层和科技页接入，无需重做。

- `XgTechTreeCopy.Name` 第19行的“长短期记忆网络（LSTM） / Long short-term memory (LSTM)”正确，保留。
- `Explanation` 当前直接取架构目录描述（第70行）；LSTM 目录原文“整句都记得住 / Keeps the whole sentence”过于绝对。实际门控仍衰减，且尚未学成的线索衰减更快，应改为相对描述。
- 当前 `Effect` 的架构通用说明只涵盖自动选型和训练。**科技页并非完全漏掉折扣**：`XgTechTreePage.cs:788–795` 已追加 LSTM 的数据门槛 ×0.8。补丁补足其生效时机、有效样本单位、叠加、参数门槛不变及进阶条件；不再改页面的现有折扣行。
- 补丁只在 Explanation/Effect 中增加 `node.id == "lstm"` 分支；不修改其他词条。原名称、空值回退、NodeMystery 和图鉴守卫全部保留。

## 可直接采用的中英文文案

**中文名称**：长短期记忆网络（LSTM）

**中文解释**：给记忆装上筛子：学过的重要线索留得更久，其余线索更快淡去。

**中文效果**：购买后，第5项能力的数据门槛降至原来的80%（300,000 → 240,000条有效样本），无需装备；参数门槛不变，同项门槛折扣相乘。加入可用结构，系统在两轮训练之间按数据和显存选择，购买不保证切换或提分。采用时，训练处理量系数为×0.8，与其他处理量系数相乘，实际卡数受取整和上下限影响。换结构后需训练并评估；能力进阶仍需满足参数、数据和本阶段训练条件，并由你确认。

**English name**: Long short-term memory (LSTM)

**Plain explanation**: A filter for memory: learned important clues last longer, while other clues fade faster.

**Effect**: Buying lowers ability 5's data threshold to 80% (300,000 → 240,000 effective samples), with no equip step. Its parameter threshold is unchanged; discounts on the same threshold multiply. LSTM becomes available for automatic selection between training rounds, based on data and VRAM; buying does not guarantee a switch or a higher score. When selected, its training-throughput factor is ×0.8 and multiplies with other throughput factors; card counts are rounded and capped at both ends. Train and evaluate after switching. Advancing still requires the parameter, data and stage-training conditions, then your confirmation.

## 生效时机、单位与源码依据

以下路径均相对工程 `Assets/LingGuangV05/XingGuang/`；完整依赖 SHA256 见 `source-hashes.json`。

| 路径与位置 | 已核对事实及文案约束 |
|---|---|
| `Core/XgCatalog.cs:160–161, 348–362, 415`；`Core/XgSim.Progression.cs:131–148` | 节点 id/target 为 lstm、Arch、序列线、阶段3、父节点 rnn。到阶段且前置/资金满足才可买；可见不是已拥有。当前基础节点价4000，经阶段×1.4和剧情×0.25，实际1400游戏货币。架构字段 cost=6 不是商店售价，不应写成¥6。补丁不重复UI动态报价。 |
| `Core/XgSim.cs:194–205, 252–279` | 一次购买写入 unlocked，重复购买被 Owned 状态阻止；随后自动配置、评估、检查桌面。没有独立的 LSTM 装备按钮。 |
| `Core/XgSim.Abilities.cs:49–67, 124–170` | 第5项能力基础D=300,000条有效样本，LSTM使D×0.8=240,000；P基础290K，即290,000个已训练参数，LSTM对P系数1。D是各数据集扣噪后再按数据权重求和，不是新赠样本、原始下载条数或准确率。折扣按已拥有且匹配能力的条目相乘；此购买一次只计一次。另有同能力参数折扣时它作用于P，不能把P与D的折扣混乘。 |
| `Core/XgSim.Abilities.cs:183–212` | P须当前形状至少训练一轮、评估达到C级后才计入。进阶要求下一能力的P/D门槛及 stageEpochs>0，并由 ConfirmNextStage 确认；购买不直接赋予能力。若已有足够P/D，只需满足现有条件，无须把所有样本再训练一遍。 |
| `Core/XgSim.AutoModel.cs:136–183, 219–260` | 自动选择已拥有、适配训练线的架构，在可用深度/宽度和显存内比较模型；epochActive 时不换型，下一次轮间配置才可采用。LSTM已买但未选中时，门槛折扣仍有效，架构处理量/接线不生效。换型通常重置学习进度；有迁移学习保留60%高于随机水平的数值进度，底层概念另按迁移规则处理。 |
| `Core/XgSim.Board.cs:68, 76–97, 191–199`；`Core/XgBoard.cs:493–519` | 采用 lstm 后接线是 GatedRecurrent。当前脑区、绝对权重≥0.3的概念所关联的原始有位置线索，每个回溯位置保留×0.99；其余×0.95，距离d为0.99^d或0.95^d。边界d=0均为1，权重恰为±0.3归重要，0.299不归重要；合成/无位置元素不走该分支。不是“记住99%的词”或“准确率99%”，更不是不丢失整句。 |
| `Core/XgSim.Board.cs:191–199`；`Core/XgCatalog.cs:160` | 活跃训练采用 LSTM 时，卡数公式含 speed=0.8，乘优化器、其他处理量和手动/自动系数，最终取整并限制为4–320张/轮；OfflineSimulation 特例固定12张，不走此系数。不能承诺每轮卡数必为原来的80%，不能写成训练耗时减少20%。 |
| `Core/XgSim.cs:351–368, 404–409`；`Core/XgSim.AutoModel.cs:334–361` | LSTM参数数=层数×宽度²×8；ParamsK以千个为单位。基础架构深度上限3，BatchNorm可加6，但仍受已买层数和显存限制。bias=0.75参与容量误差公式，span=80参与需顺序任务的记忆跨度惩罚，均不是直接加准确率，也不保证最多/必定记住80字。 |
| `Core/XgSim.Progression.cs:228–245` | 单卡距离惩罚为accuracy×0.98^distance（distance>0）；每距离单位相乘，而非每单位减2个百分点。与大脑线索的0.99/0.95衰减属于不同量。 |
| `Core/XgSim.cs:517–541, 639–669`；`Core/XgSim.Progression.cs:255–270` | 当前评估读数来自存储准确率和CeilingAcc，训练用StepAccuracy推进，同时训练Board。旧LegacyEvaluate才调用ProgressionAccuracy；其长句0.98^10不是当前主评估的额外保证。GpuUtilization的0.31是单独接口，未据此承诺玩家看到GPU恰占31%。 |
| `Core/XgSim.Progression.cs:272` | “长句”等桌由阶段/数据包/已拥有数据条件开放；买LSTM只是后续节点前置之一，不能文案宣称立刻免费解锁所有长句或翻译内容。 |

LSTM架构字段 instability=0.1 不是10%的实际失败率；不能把现实AI三道门的训练算法、永久记忆或真正翻译能力自动视作本游戏效果。这里只描述已实现的简化门控与数值逻辑。

## 神秘性与整合

`XgTechTreeCopy.cs:15, 48` 在词条特例之前保留 NodeMystery/IsAtlas 回退；`XgTechTreePage.cs:758–764` 在完整说明之前返回神秘提示。LSTM本身没有NodeMystery标记，未到阶段由NodeVisible控制。补丁不显示隐藏节点、未发生图鉴、后续能力名称或提前放宽解锁。

`changes.patch` 目标只有 `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`，基准SHA256：

`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`

补丁包含三行上下文。整合者若已修改同一方法，应按LSTM分支语义合并，禁止用旧文件覆盖其他交付。固定数值均针对本次源码；若常量改变，应同步调整文案或改成从AbilityDiscounts/目录取值。

## 验证分层

- 静态核对：已沿节点→购买→折扣→轮间选型→训练/评估→进阶确认读取代码，已区分当前与旧评估路径；不是实际玩法测试。
- 补丁与依赖复核：执行结果另见 `verification.md`。
- 实际运行：未运行C#、Unity或游戏，未做实际购买/训练，不能记为运行PASS。
- 视觉验收：未进行。此双语效果较长，整合后需要确认提示框滚动/最小窗口文字可读；已有底部折扣行有意保留。

待主聊天统一验证的真实边界：购买后未选中LSTM仍获D折扣；训练中购买不改变本轮架构；无新训练P不能因购入而增加；D=239,999/240,000与P不足/足够的进阶条件；stageEpochs=0/1；未确认不得跳阶段；门控d=0、权重±0.3和0.299；吞吐触及4/320张上下限与离线固定12张；中英文隐藏节点无泄漏。以上为待执行验收，不声称已通过。

本任务仅核对LSTM，不将全局原则、整份文档或其他词条标记完成。
