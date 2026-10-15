# tech-013 — Dropout（随机失活）文案核对

结论：需要小范围文案修正。现有正式名称正确，解释方向基本正确；效果只写“跳过部分线索更新”，遗漏实际的 20% 规则、组合重复强化限制、两个 ×0.5 项以及购买自动启用的时点。交付 changes.patch，仅替换 XgTechTreeCopy.cs 中 dropout 的解释与效果两条分支；未修改生产源码或模拟数值。

## 范围与已有记录

仅核对本条节点和必要调用链。已查看队列、进度中当前“原文第 3 行词条文案契约”的任务记录；本任务目录此前不存在交付。未启动 Unity 或游戏，未接触真实玩家存档，未提交、推送或发送其他聊天消息。不代表整份策划、其他词条或全局原则完成。

## 可直接采用的中英文三项文案

**正式专业名称**

- 中文：随机失活（Dropout）
- English: Dropout

**外行能懂的解释**

- 中文：训练时让部分线索暂停更新，减少只靠熟题背答案。
- English: Pause updates to some clues during training to reduce reliance on memorized examples.

**玩家看得见的真实效果**

- 中文：购买后自动生效，无需装备，同项不重复叠加。后续训练按固定规则跳过约 20% 的匹配线索更新；组合仍在积累时，同一题卡不能连续强化它。背答案造成的退步概率分量、训练与评估差距的计算系数各 ×0.5；总退步概率和显示差距不保证减半。与其他技巧按各自规则共同计算，评估成绩仍需训练后观察。
- English: Automatic after purchase; no equipment step or duplicate bonus. Subsequent training skips updates to about 20% of matched clues using a fixed rule. While a combination is accumulating progress, the same card cannot strengthen it twice in a row. The overfitting component of drop probability and the coefficient for the training/evaluation gap each become ×0.5; the total probability and displayed gap need not halve. Other techniques apply by their own rules; evaluate after training to see the outcome.

补丁中的 20% 从现有常量 `XgBoard.DropoutRate * 100` 格式化生成；未增加新参数。正式名称保持原样。

## 节点、购买和生效合同

| 项目 | 已确认的实际行为 |
| --- | --- |
| 类型 | `dropout` 是 Research，kind 为 Dropout，不是可装备架构。 |
| 发现/解锁条件 | 第 3 阶段出现；需持有父节点 `mlp` 且满足资金条件。仅可见或可购买不产生效果。 |
| 价格 | 节点构造价 400；当前 `NodeCost = Ceiling(cost * .25)`，实际 ¥100。旧研究目录的 cost=10 不用于此节点购买价。价格已有独立 UI，不写入效果文案。 |
| 持有 | `BuyNode` 成功扣款后加入 `S.unlocked`；`Has("dropout")` 判断持有。立即自动启用，不另装备，也没有独立开关。重复购买返回 Owned，持有布尔值不会使乘数重复叠加。 |
| 当下变化 | 购买会调用 `Evaluate`，训练成绩与评估成绩之间的计算差距可以立即改变；不会仅因为这个乘数直接给现有 `valAcc` 加分。 |
| 后续训练 | 后续 `Knobs` / `DropRisk` 调用读取持有状态。实时训练在轮次结算调用 `TrainEpoch`；研究项未被“训练期间禁止购买”条件覆盖，所以轮次中途买入也可作用于尚未结算的一轮，不能笼统写成“下一轮开始才生效”。 |
| 大脑与成绩 | 默认 `UseBoard=true` 时，TrainBoard 更新真实概念；同轮 StepAccuracy 根据概率推进评估成绩。概念抑制不是把当前评估分数直接提高固定比例。 |

## 数值单位与叠加方式

1. **约 20% 匹配线索更新被跳过**：`DropoutRate=.2` 是按题卡 seed 与概念 id 的固定哈希规则决定的阈值。不是每张卡刚好 20%，不是每次重抽，也不是删除概念或推理时关闭 20% 神经元。被固定的概念先跳过；Dropout 跳过的是其余匹配概念的权重、强度及训练次数更新。当前 Score/预测在这段跳过之前计算。
2. **组合重复强化限制**：当同一组合 `link.last == card.seed && link.c > 0` 时跳过累积。只禁止“上次来源仍是这张卡且仍有正进度”的重复强化；其他卡介入或进度归零后，不能宣称这张卡永久无效。组合创建还受现有深度、连接、误差、阈值等条件约束。
3. **退步概率的过拟合分量 ×0.5**：`r.overfit = .24 * Clamp01(log10(max(1,参数/有效样本))/3) * .5`。单位是概率，范围由 0–0.24 变为 0–0.12，即上限 24% → 12%，不是减 50 个百分点。噪声、漂移、跳跃、正常波动项不由 Dropout 减半。
4. **总风险叠加**：先合计这些分量，再按已持有 BatchNorm 乘 .8，最后封顶 .45。数据增强通过有效样本数改变过拟合基数；不同技巧不是统一相加百分比。这里仅说明 Dropout 的交互，不修改或验收其他词条。
5. **OverfitScale ×0.5**：这是无单位乘数；Evaluate 将它与进度项相乘，再对训练—评估准确率差距封顶 .2，并受数据集上限和后续近亲繁殖规则约束。因此显示差距不一定严格减半。LegacyEvaluate 也复用此系数，但它注明为旧闭式曲线，不能当成当前直接加分的玩法。

## 原文件证据

行号对应生成补丁时的源码；完整 SHA256 清单见 source-hashes.json。

- `Assets/LingGuangV05/XingGuang/Core/XgCatalog.cs:306,369,414,635`：ResearchNode 创建、stage=3、parent=mlp、节点 cost=400、kind=Dropout。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.cs:188,194,252,261,277,503,526,657,665`：持有与状态、扣款入库、购买后 Evaluate、OverfitScale ×0.5、差距封顶、训练与评估推进。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Progression.cs:132,140,154`：当前价格、可见性、阶段阻挡。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.Board.cs:41,94,110,211`：默认启用大脑模拟、持有即开启 dropout、训练参数在实际喂卡时读取。
- `Assets/LingGuangV05/XingGuang/Core/XgBoard.cs:474,477,535,548,559,583,588`：20% 常量、确定性遮罩、先评分后跳过更新、相同 seed 的组合进度保护及阈值后归零。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.AutoModel.cs:70,454,460,467,468,495`：过拟合风险减半、BatchNorm 乘在合计之后、45% 封顶、StepAccuracy 消费风险。
- `Assets/LingGuangV05/XingGuang/Core/XgSim.EpochAction.cs:49`：实时轮次结算时调用 TrainEpoch。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:15,30,51,92,176`：名称与 Mystery/Atlas 早退保留；仅修改 dropout 解释与效果。
- `Assets/LingGuangV05/XingGuang/Core/XgTechTree.cs:127` 与 `Desktop/XgTechTreePage.cs:428,758,772`：未到阶段的节点不显示，神秘节点先返回占位提示，然后才进入普通词条内容。不改变此路径，不增加未来节点、后继词条提示。

补丁目标：`Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs`

补丁基准 SHA256：`308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8`

changes.patch 包含两个各带三行前后上下文的精确 hunk。并行修改可能使整文件 SHA 改变；整合时先核对两处 dropout 原文及相关依赖，不能覆盖整文件。

## 静态边界分析与验证状态

已完成：源码调用链核对；补丁只触及 dropout 两条返回文案；`git apply --check --ignore-space-change` 返回 0（只校验，不应用）。

以下为按现有公式做的静态推导，不是运行测试结果：

- 参数/有效样本 ≤1 时，过拟合风险本来就是 0，买入仍为 0；不能承诺所有情况下风险都会下降。
- 过拟合分量 .24、其他项只有普通波动 .03 时，总风险 .27 → .15，绝非 .135；同时有 BatchNorm 则 .216 → .12。
- 其他项合计 .45 且过拟合分量 .24 时，买入前后仍被封顶到 .45；UI 风险可以不变。
- 未封顶差距项 .4 → .2，经 .2 封顶后显示可同为 .2；不能保证可见差距减半。
- 同卡同概念固定选取；小匹配集合可以一条也不跳过或全部跳过，20% 是规则比例而非卡内配额。
- 同卡重复组合只有 last 相等且正进度时被拦截，c=0 的边界不被该条件拦截。

实际运行：未运行 Unity、C# 编译或游戏测试，未标注 PASS。

视觉验收：未进行。中文和英文效果较长，需主聊天在词条浮层检查换行、裁切、滚动与不同语言宽度。

## 待整合与待验证

整合负责人只需核对当前依赖并应用这两个文案 hunk；无需改任何数值、架构、目录或存档。统一编译后，在临时测试状态覆盖：未发现阶段隐藏；已发现未购买；成功购买与重复购买；训练中途购买后结算；不同样本比与风险封顶；相同卡重复与组合归零；中英文浮层。以上运行与视觉工作由主聊天执行。依赖结束复核：10 个源码文件 SHA256 均与补丁基准快照一致。
