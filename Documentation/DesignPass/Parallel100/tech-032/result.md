# tech-032：chainrule 文案核对

日期：2026-10-13（Asia/Singapore）。范围仅为当前 chainrule 节点。

## 结论

当前 XgTechTreeCopy 的名称、解释及 ×1.1 处理量说明与生产实现一致，无需生产改动，不提供 changes.patch。以下详版是可选采用文案，不是缺陷修复。未修改 Assets、模拟数值、其他词条或全局清单。

任务简称“链式法则”对应内部 ID `chainrule`，但当前 XgCatalog 正式名称是“自动求导 / Automatic differentiation”，节点种类为 Research、研究种类为 ManualPay。链式法则是数学规则，自动求导是相关计算技术，不能只据 ID 将正式名称改成 Chain rule。现有专业名可保留，不强制为了术语偏好改成“自动微分”。解释是概念教学，不代表游戏已实现完整自动微分框架；该道具的专属代码效果仅为处理量乘数。

已核对已有记录 `Documentation/DesignPass/Agents/skilltree-task-001.md`，其中已经说明“处理量、取整和上下限”，此次未发现 chainrule 独立修复记录，也不重复修补共用 Speed 文案。

## 可直接采用的中英文文案

### 中文

**正式名称：** 自动求导

**通俗解释：** 由程序算出每个参数该往哪边改，省去手工推导。

**真实效果：** 购买后自动启用，无需装备。视觉与序列的手动、自动训练，其每轮处理量计算值 ×1.1（提高 10%），与其他适用的处理量倍率相乘；本项只计算一次。实际每轮处理卡数还受算力、模型、取整和上下限影响，可能保持不变。效果在购买后的训练结算中体现，不缩短每轮耗时，也不直接增加准确率；成绩须训练后评估。

### English

**Name:** Automatic differentiation

**Plain explanation:** Let the program calculate how each parameter should change.

**Effect:** Automatically active once purchased; no equipment step is needed. Multiplies the calculated cards per epoch by 1.1 (+10%) for manual and automatic training on both the vision and sequence tracks. Multiplies with other applicable throughput bonuses and applies only once. Actual cards per epoch still depend on compute, the model, rounding and lower/upper limits, so the count may stay unchanged. Applies when training settles after purchase. Does not shorten an epoch or directly raise accuracy; evaluate after training to see the score.

上述文案用于正常在线训练。离线模拟 `OfflineSimulation` 分支固定每轮 12 张，绕过该倍率；若界面用于展示离线结算说明，应另附“离线模拟每轮固定 12 张，不应用此处理量倍率 / Offline simulation uses a fixed 12 cards per epoch and does not apply this throughput multiplier.” 不应以离线模拟测出的卡数否定正常训练效果。

## 源码证据与时点

下列路径均相对 `Assets/LingGuangV05/XingGuang/`；校验摘要见 source-sha256.json。

| 证据 | 核对结果 |
| --- | --- |
| Core/XgCatalog.cs:306–309、396、619 | ResearchNode 从 ResearchItem 获取名称；chainrule 为阶段 2、前置 backprop 的一次性研究道具，正式名称自动求导，不是架构。 |
| Core/XgSim.Progression.cs:140–148、154–164；Core/XgSim.cs:194–205 | 阶段可见和前置拥有只是购买条件，不会提前给倍率。已拥有时状态为 Owned，不能重复购买。 |
| Core/XgSim.cs:188、252–279 | Has 检查 unlocked；购买成功写入 unlocked，即使没有额外装备步骤也会生效。购买随后有通用模型配置与评估，但不能把这些调用描述为本项直接提分。 |
| Core/XgSim.Progression.cs:268–269 | Has(chainrule) ? 1.1 : 1，与其他研究因子相乘；不按等级累加，无架构或训练线限定。两个 +10% 因子相乘是 ×1.21，而不是 ×1.20。 |
| Core/XgSim.Board.cs:42–43、191–199 | 正常每轮卡数公式乘 ProgressionSpeed；手动连击/自动半效另外相乘。Math.Round 后限制在 4–320 张。OfflineSimulation 提前返回 12。倍率无量纲，最终可见量单位为“张/轮”，不是准确率百分点。 |
| Core/XgSim.EpochAction.cs:10–25、40–50；Core/XgSim.cs:639–678 | 开始一轮时不锁定卡数，完成时 TrainEpoch 才计算当前 CardsPerEpoch。若一轮中途购买，且该轮随后正常结算，也按届时拥有状态计入。无需先训练一轮来激活。DurationFor 不含 chainrule，因此无每轮时间减少承诺。训练有资源/样本阻挡，购买不会绕过。 |
| Core/XgSim.cs:554–558 | StepsPerSecond 公式也包含 ProgressionSpeed；实际轮次收益应以当前 TrainEpoch → CardsPerEpoch 路径解释，不能以方法名推导额外实时加速功能。 |
| Core/XgSim.Finale.cs:75–84 | chainrule 不在 ScaleItems，无该道具专属预训练有效规模倍率，不添加此类效果。 |
| Core/XgTechTreeCopy.cs:15、42、51、82、125–126、160 | 名称默认沿用目录；当前解释正确。Speed(1.1) 明确处理量、取整、上限和评估，没有承诺每轮卡数或成绩必增 10%，可以保留。 |
| Core/XgTechTree.cs:127–130、146；Desktop/XgTechTreePage.cs:428、758–772 | 页面依 NodeVisible 显示节点；神秘节点先回退提示，再处理正常文案。chainrule 本身按阶段显示，并无单独 Mystery 特判。无需改变发现机制。 |

可见文案不列出任何未发现后继节点、专属奖励或解锁秘密；也不以本说明绕过现有阶段隐藏。购买条件交给既有动态条件区，避免写死费用及前置名称。

## 验证与待整合

- 已完成：静态调用链核对、现有记录对照和来源 SHA256 记录。没有运行 Unity、游戏、C# 编译或真实存档；不声称运行 PASS 或视觉验收完成。
- 待整合：保留当前简版即可；若版面允许，可采用以上详版。没有必须应用的源码补丁。
- 待主聊天实际验证：正常训练中购买前后，固定模型、算力、连击等条件后比较 CardsPerEpoch；检查整数台阶（例如原始值 4.0 → 4.4 仍为 4）、上下限（≤3.0 → ≤3.3 仍为 4；≥320 → ≥352 仍为 320）、手动/自动与两条训练线、叠加其他研究、重复购买被拒绝，以及轮中购买后结算。上述数字是边界用例设计，不是已执行测试结果。
- 待主聊天验证：OfflineSimulation 固定 12 的例外；购买不改变本项专属准确率上限、不缩短每轮时长；中英文页面实际排版及未发现节点仍隐藏。
- 结束前的依赖复核结果见 dependency-recheck.md。不能据此标记全局原则或整份策划完成。

阅读边界备注：首次关键词检索误命中既有设计清单一行；未继续读取该清单或新增策划正文，本结论只以生产源码和既有任务记录为依据。
