# tech-039 · gradclip（梯度裁剪）

结论：名称与通俗解释正确，保留；效果说明需要补齐自动启用时机、真实数值及风险分量范围。交付 changes.patch，仅替换 XgTechTreeCopy.Effect 的 gradclip 分支，不改生产代码或模拟数值。

本任务只核对已授权的三项文案契约，不代表其他词条、后续策划条目或整份文档完成。已查进度.md、队列.md 的当前任务记录和 Parallel100 中的 gradclip 记录；已有 tech-011 只修 irnn，未发现重复的 gradclip 专项补丁。本目录此前无产物。未启动 Unity/游戏，未操作玩家存档、提交、推送、打包或其他聊天。

## 可直接采用的中英文文案

正式名称（沿用目录定义）：
- 中文：梯度裁剪
- English: Gradient clipping

通俗解释（沿用现文）：
- 中文：一次改动太大时把它截短，减少训练突然失稳。
- English: Trim oversized updates to reduce sudden training instability.

真实效果（补丁中的 0.5 从 XgBoard.ClipLimit 动态格式化）：
- 中文：购买后无需装备，系统在下一次训练结算前自动启用。每张训练卡对单个权重的拉动不超过 ±0.5（权重单位）。失稳步长阈值的基础项 ×2，并取消循环结构的额外阈值折减；仍可能失稳。购买后，大幅进步带来的回落风险分量 ×0.7，即该分量减少 30%，并与其他减险倍率相乘；不是总风险减 30 个百分点。不可重复购买叠加，不直接增加准确率。
- English: No equipment step is needed after purchase; clipping is enabled automatically before the next training settlement. Each training card caps the pull on each weight at ±0.5 weight units. The base instability threshold is multiplied by 2, and extra threshold reductions for loops are removed; instability is still possible. Once purchased, the drop-risk component caused by a large gain is multiplied by 0.7 (30% less for that component), stacking multiplicatively with other risk reductions; total risk does not fall by 30 percentage points. Cannot be purchased repeatedly for stacking bonuses and does not directly add accuracy.

拥有状态不提供重复购买或等级叠加；裁剪本身会用于每张训练卡的权重拉动，不是一次性消耗。

## 源码证据与生效合同

以下路径以 Assets/LingGuangV05/XingGuang 为基准，行号为本次快照。

| 文件与行号 | 代码事实与文案含义 |
| --- | --- |
| Core/XgCatalog.cs:306、369–370、414、633–634 | gradclip 是研究节点而非可装备架构，正式名称来自 Research 定义；阶段 3，前置 rnn。研究条目的 cost=5 不是实际树节点售价。 |
| Core/XgCatalog.cs:348–356；Core/XgSim.Progression.cs:131–148；Core/XgSim.cs:194–205 | 节点可见不等于拥有或产生效果。实际当前价格 ceil(round(150×1.4)×0.25)=¥53 游戏经费；前置与资金须满足。价格由既有 UI 显示，不在效果中硬编码。 |
| Core/XgSim.cs:188、252–276 | Has 检查 unlocked；成功购买写入一次，已拥有时不可重复购买。购买调用 AutoConfigureAll 并评估现有 run。无需另行装备，也无预训练激活步骤。 |
| Core/XgSim.AutoModel.cs:187–240；Core/XgSim.Board.cs:82、94–115 | unlocked.Count 进入配置缓存键。自动配置设 run.clip=ClipOwned，实际旋钮仍要求 Has(gradclip) 且 run.clip。空闲 run 在购买时配置；正在计时的 run 暂跳过。不限 RNN，也不保证购买后采用某个指定架构。 |
| Core/XgSim.EpochAction.cs:18、44–49；Core/XgSim.cs:641–665 | 计时结束先清除 epochActive，再调用 TrainEpoch；后者在实际训练卡与成绩结算前再次 AutoConfigure。因此计时途中购买可用于这次尚未结算的轮次，不能一概写成“下一轮开始才生效”。 |
| Core/XgBoard.cs:222、535–562 | ClipLimit=0.5；对每个非固定、未被 dropout 跳过的匹配概念，R1 pull 被逐项夹在 [-0.5,+0.5]。单位是无量纲权重增量/训练卡/概念，不是百分比、分数、整轮总和或梯度向量范数。不是实现了真实神经网络的全局范数缩放。 |
| Core/XgBoard.cs:130–139、221、553 | TearAt 的基础 .8 乘裁剪系数 2，与 batchNorm 的 1.5、优化器 steadiness 等按公式组合，再除深度因子。启用裁剪取消 loop 的 .75 折减及 IdentityLoop 的额外 .5 折减。同配置下非循环阈值比为 2，普通循环为 2/.75≈2.667，满足 IdentityLoop 的循环为 2/(.75×.5)≈5.333；不能说所有模型最终阈值恰好翻倍。 |
| Core/XgBoard.cs:552–562；Core/XgSim.Board.cs:211–232；Core/XgSim.cs:657 | 发散标记判断 lr×abs(error)>TearAt 在裁剪之前执行，等于阈值不触发；仍可能失稳。TrainBoard 的 torn 返回值在 TrainEpoch 中未作为成绩损失条件使用，不能把此阈值收益直接写成成绩不再下降。 |
| Core/XgSim.AutoModel.cs:454–469、492–525 | DropRisk 直接按 Has(gradclip) 将 r.jump 乘 .7，购买后下次查询即使用；实际回落抽样在 StepAccuracy。这里不要求 run.clip=true，不受 UseBoard 分支限制。只降低大幅进步分量；与 warmup 的 .5 相乘后为原分量 .35，总分量相加后再受 batchnorm 的 .8 与总风险上限影响。补丁不透露其他未发现节点名称。 |
| Core/XgSim.cs:560–582 | 还存在 Hazard 的架构 instability×.2，以及 UseBoard=false 时 SafeLr 的相同折减。此次源码检索未发现 Hazard 的实际训练调用；它不等于当前主训练总崩溃风险降低 80%，故不写进玩家效果。非 Board 安全学习率分支作为维护证据保留。 |
| Core/XgTechTreeCopy.cs:15、45、51、91、175；Core/XgTechTree.cs:127–130、146；Desktop/XgTechTreePage.cs:758、772 | 正式名称走目录回退，通俗解释正确。Effect 现文只有笼统限制与风险提示，没有数值、时机和回落分量。补丁保持 mystery 回退及 NodeVisible 路径；未到阶段的 gradclip 继续隐藏，不添加后续词条名称或解锁收益。 |

裁剪只限定 R1 拉动，并不声称限制 R2 概念组合的 link.c、所有状态变化或整轮的累计权重变化。不会保证绝不出现 NaN、准确率必涨或总回落概率固定下降 30%。

## 补丁与整合

- 目标：Assets/LingGuangV05/XingGuang/Core/XgTechTreeCopy.cs:175。
- 源文件 SHA256：308FE4CD23D4410107D15670FF04520EFFC6CF59AB44B6370837053ECDDADFE8。
- changes.patch 带目标、SHA256、相邻行上下文，唯一改动是 gradclip 效果分支；名称、解释无需补丁。
- dependencies.json 保存本次依赖文件的 SHA256。整合前若目标或模拟依赖变化，请重新核对对应分支，勿将整份旧文件覆盖生产文件。并行词条可能改变相邻行，必要时精确取本分支替换。
- 静态补丁适用性检查：git apply --check 已执行成功；没有实际应用到生产目录。

## 待验证的真实边界（尚未运行）

1. 未到阶段时不可见；到阶段但未拥有 rnn 时不可买；只可见而未购买时无裁剪/风险收益。未购买而人为置 run.clip=true 也不得生效。
2. 空闲购买后 run.clip 自动开启；计时中购买暂不更改该 run 的配置，到当前计时轮次结算前配置开启。以后各轮持续生效，重复购买被拒绝。
3. 用可更新的匹配概念控制未裁剪 pull 为 ±0.49、±0.5、±0.51，检查分别保留、保留、截到 ±0.5；对 pinned 或 dropout 跳过概念，不应为了“验证裁剪”强行发生更新。多卡累计可超过 0.5。
4. 同形状模型中分别检查非循环、普通循环、满足 IdentityLoop 的循环阈值比为 2、8/3、16/3。构造 lr×abs(err)=TearAt 及略大于阈值，确认严格大于才标记发散，且大步即使 pull 被截断仍能标记发散。
5. 用有效数据集，固定其他风险项：归一化 jumpPoints≤30 时 r.jump=0；90 时由 .05 变 .035；≥150 时由 .10 变 .07。这里 jumpPoints 是 lastGain×1000/range，非原始准确率百分点。组合 warmup 时峰值 .035；再经 batchnorm 对总和乘 .8，该项贡献 .028。观察总风险上限饱和时，总风险可能不变。不得把单项变化当总概率断言。
6. 两语言分别检查标题、正文换行与滚动；未来阶段的 gradclip 不显示。正文较长，视觉可读性由整合负责人在实际 UI 中验收。

实际编译、模拟运行及视觉验收均未执行；以上是待验收用例，不是 PASS 记录。统一编译、场景截图和运行验证留给主聊天。只完成本节点的静态审阅与文案交付。

结束复核：dependencies.json 中 10 个依赖文件 SHA256 均与基线一致；再次执行补丁适用性检查成功，并确认仅删除/新增 gradclip 效果分支各一行。这些仅为静态检查结果。
