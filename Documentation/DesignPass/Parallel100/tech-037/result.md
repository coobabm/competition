# tech-037：RMSProp 词条核对

结论：需要精确补充文案，生产实现不改。现有名称 RMSProp 正确；×1.6 和只选择最快已拥有优化器正确。当前解释没有区分算法原理和游戏实现，效果遗漏自动选用时点、失稳步幅阈值 ×1.15 及其叠加规则。交付 changes.patch，仅修改 RMSProp 解释，并在通用优化器效果分支之前添加 RMSProp 专属文案；其他优化器分支不变。

## 范围与已有任务记录

核对了队列、进度中现有第 3 行词条契约的任务记录，以及 tech-013 的相关交付，避免改写其他任务负责的 Dropout 文案。tech-037 目录此前没有交付。本任务只覆盖 rmsprop，不代表其他词条或整份策划已完成。只在本专属目录写入交付；未修改生产文件、模拟数值、全局清单或存档，未启动 Unity/游戏，也未发送其他聊天消息。

## 可直接采用的中英文文案

名称保持现有正式算法名，无需修改：

- 中文：RMSProp
- English: RMSProp

通俗解释：

- 中文：算法原理：按每个参数最近收到的调整信号，分别控制学习步幅。
- English: Algorithm principle: use each parameter's recent gradient signals to set its learning step size.

真实效果（与补丁一致，×1.6 从研究目录动态读取）：

- 中文：购买后由系统自动选用已拥有的最快优化器，无需装备。RMSProp 被选用时，两条训练线相对基础 SGD 的训练处理量 ×1.6；同配置下的训练失稳步幅阈值 ×1.15。优化器之间不叠加，处理量与其他训练倍率相乘，阈值与已启用的归一化、裁剪倍率相乘。实际每轮卡数受算力、取整和上限影响；效果在后续训练结算时采用，成绩需训练后评估。
- English: After purchase, the fastest owned optimizer is selected automatically; no equipment step is needed. While RMSProp is selected, both training tracks have a throughput factor relative to base SGD of ×1.6; the step-size threshold for training instability is ×1.15 at the same configuration, relative to base SGD. Optimizers do not stack: throughput multiplies with other training factors, and the threshold multiplies with active normalization and clipping factors. Cards per epoch depend on compute, rounding and caps. These effects apply at subsequent training settlements; evaluate after training to see the score.

## 节点与时点合同

| 项目 | 实际行为 |
| --- | --- |
| 类型 | `rmsprop` 是 Research / Optimizer，并非网络架构或装备。 |
| 可见与购买 | 第 3 阶段可见；父节点为 `momentum`，购买还需足够资金。仅发现、看到或满足前置条件不产生加成。 |
| 价格 | 节点构造价 1200；当前 `NodeCost = Ceiling(cost * .25)`，实际 ¥300。研究目录中的旧 cost=30 不是节点成交价。价格由独立 UI 展示，不重复塞入文案。 |
| 持有 | 成功扣款后加入 `S.unlocked`，无需下载或装备。重复购买被 Owned 状态阻止。 |
| 自动选择 | 所有已拥有 Optimizer 中取 speed 最大者；基础 SGD=1，Momentum=1.3，RMSProp=1.6，Adam=2。若已有更快优化器，RMSProp 不再贡献独立速度或稳定性。效果文案不提前列出后继节点名称。 |
| 即时配置 | 购买调用 AutoConfigureAll / Evaluate；未处于活动轮次的模型会重新计算安全学习率。它不会仅因购买直接获得固定分数奖励。 |
| 训练生效 | `CardsPerEpoch` 和 `Knobs` 读取当前选中的优化器。正在进行的实时轮次在结束时才调用 `TrainEpoch`，因此训练中买入也可能影响该轮尚未发生的结算，不能写死成“下一轮开始才生效”。活动轮次的自动配置会延迟到结算。 |
| 神秘性 | 保留 Name/Note 的 Mystery、Atlas 早退及 NodeVisible 阶段过滤。不改变任何显示条件；未持有的 RMSProp 在第 3 阶段前不显示。 |

## 所有效果与数值单位

1. **训练处理量 ×1.6**：是普通训练每轮卡数计算式的无单位乘数，两个训练轨道、手动与自动结算共用。不是每秒 +1.6、准确率 +60%，也不是一轮时间缩短为 1/1.6。计算式还乘其他速度、架构、连击/自动化等因子，再四舍五入并限制到 4–320 张/轮。相对于已持有 Momentum 的未取整基数提升为 1.6/1.3≈1.230769（约 +23.08%），不是再乘 1.6。`OfflineSimulation` 分支固定 12 张/轮，绕过该倍率；这属于内部离线模拟边界，不能用它验证普通训练处理量。
2. **失稳步幅阈值 ×1.15**：默认 `UseBoard=true` 下，`OptimizerSteadiness` 写入 Knobs.steadiness，并进入 `TearAt`。同一模型、相同其他开关时，相对 SGD/Momentum 提高阈值 15%。阈值针对 `lr * Abs(error)` 的无量纲步幅，不是将退步概率减 15%，也不保证任何学习率都安全。完整式为 `.8 × (batchNorm?1.5:1) × (clip?2:1) × 1.15 / 深度因子`，之后仍受未裁剪循环网络及 IdentityLoop 的条件修正。
3. **可能的安全学习率变化**：SafeLr 按离散 RateValues 寻找能够承受最大误差 2 的档位，因此阈值提高不保证学习率换档。购买令 unlocked.Count 改变，自动配置缓存键也随之变化；不可把同配置阈值加成等同于重新配置后所有指标精确同比增加。
4. **学习率标签缩放接口**：选中 RMSProp 时 `RateDisplayScale=.01`，例如内部 `.1` 对应标签 `.001`。Knobs.lr 仍直接读 RateValues；这不是实际训练步幅除以 100。对 `Assets/LingGuangV05` 的静态调用检索未找到 RateLabel 的生产调用方，当前 XgTrainPage 展示 OptimizerName。因此交付文案没有宣称玩家当前页面一定看见标签缩放。
5. **目录 stability=2 的旧路径**：Stability 将所选优化器的 stability 与持有 BatchNorm 时的 1.5 相乘；用于 Hazard 和 UseBoard=false 的 SafeLr 路径。Hazard 没有检索到生产调用方，默认 Board 路径以 1.15 的 TearAt 为准。不能写“当前训练稳定性翻倍”或“爆炸概率减半”。StepsPerSecond 同样存在 ×OptSpeed 的旧公式，但未检索到生产调用方，不将其宣称为当前每秒吞吐量。
6. **算法和模拟的边界**：真实 RMSProp 以梯度平方的移动平均缩放更新。当前 Board.Train 的权重更新仍为统一 `k.lr * err * activation * layerFactor`，并没有为各参数保存 RMSProp 的平方梯度累积。因此解释明确标为“算法原理”；游戏实际效果只陈述已接入的处理量、阈值和配置行为。不将科学原理当作已运行的完整算法。ScaleItems 中没有 rmsprop，不增加预训练有效规模倍率。

## 原文件证据

以下路径均相对工程根目录 `C:/Users/xomem/Documents/ChatGPT/games/competition-20261011`，行号对应本次快照；完整 SHA256 清单见 source-hashes.json。

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:306,369,414,641`：节点类型、阶段、父节点、节点定价、speed=1.6/stability=2、正式名称。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:188,194,252,318,335,344,346`：持有、购买、最快优化器选取、标签换算和两种稳定性参数。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:554,561,568,639`：旧速度/风险接口、SafeLr 分支、训练卡数与结算调用顺序。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:131,140,154`：实际价格与阶段过滤。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs:41,76,94,191,211`：Board 默认开启、内部学习率、Knobs、4–320 张卡数、离线固定 12 张、训练参数即时读取。
- `Assets/LingGuangV05/XingGuang/Core/XgBoard.cs:130,221,535,553,560`：阈值公式、基础 .8、严格大于阈值才失稳、现有权重更新方式。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.AutoModel.cs:187,199,219,244,454`：配置缓存键、自动安全学习率、活动轮次推迟配置、当前 DropRisk 没有直接使用 RMSProp 稳定性。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.EpochAction.cs:44,49`：实时轮次结束解除 active 后调用 TrainEpoch。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:15,51,89,155`：现有保护逻辑、RMSProp 原解释与通用优化器效果。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs:127`、`Desktop/XgTechTreePage.cs:758,772`：可见性过滤及神秘节点提前返回。
- `Assets/LingGuangV05/XingGuang/Desktop/XgTrainPage.cs:596`：当前优化器名称的玩家提示。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Finale.cs:75`：预训练倍率表没有 RMSProp。

## 补丁与验证状态

目标文件：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`。

补丁基准 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`（也写在 changes.patch 头部）。两个 hunk 均带上下文；不修改名称、模拟数值、公共 Speed 辅助函数或其他节点文案。文案中的 1.15 是已核实的当前实现常量的描述，不是新增模拟参数。

静态检查：已核对实际调用链，并运行 `git apply --check --ignore-space-change`，返回 0；只检查补丁适配，没有应用。未做 C# 编译、Unity 或游戏运行，未标注运行 PASS；视觉验收未进行。

静态边界推导（不是实际运行结果）：

- 普通计算基数为 10 时，SGD=10、Momentum=13、RMSProp=16 张，证明升级的基准区别。
- 基数为 1 时，SGD 与 RMSProp 都由下限夹到 4 张；基数为 400 时都封顶 320 张。实际显示不保证 ×1.6。
- 同配置浅层非循环网络的阈值从 .8 到 .92。步幅 .9 原先失稳、RMSProp 下不失稳；步幅恰等于 .92 时不满足严格大于条件；超过 .92 仍可能失稳。
- 同配置启用 BatchNorm 和裁剪时，阈值分子为 .8×1.5×2×1.15=2.76，体现乘法关系；不等于加 15 个百分点。
- 已有更快优化器时，选择结果不变，购买/持有 RMSProp 不叠加 1.6 或 1.15。
- 只有发现或解锁购买条件而未成功扣款时，没有持有标记、没有效果；训练结算前买入与结算后买入应分别验证。

## 待整合与待验证

整合负责人核对 SHA 与 RMSProp 两处上下文后应用文案 hunk。与其他并行词条补丁共享文件时，合并局部变更，不覆盖整文件。统一编译和实际测试由主聊天负责：覆盖阶段 2 隐藏、阶段 3 前置未满足/满足、成功与重复购买、无装备步骤、两轨手动/自动训练、已拥有更快优化器、训练中途购买、卡数上下限与阈值边界。使用临时状态，不读取真实玩家存档。

视觉验收需检查中英文词条展开后的换行、裁切和滚动；本效果说明较长，未声称已经通过页面验证。源码结束复核结果见 dependency-recheck.json。
